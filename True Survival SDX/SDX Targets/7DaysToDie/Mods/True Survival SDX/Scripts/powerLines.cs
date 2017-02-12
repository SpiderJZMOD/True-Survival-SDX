using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// "repeater" or accumulator class
/// basically it acumulates power units, and feeds the connected machines
/// it only sends requests to the parent when it's own power units ran out.
/// it also improves performance of the line, since 1 souce power unit will be able to feed 1 branch for a while, like an amplifier
/// EXPENSIVE to build and REMEMBER: the more valves there is in a line, the more gaz will be required from the tank to keep all branches. This escalates fast!
/// they can only be placed near pipes
/// but it acumulates power
/// Mortelentus - 2016
/// </summary>
public class BlockValve : Block
{
    private bool disableDebug = true;

    private int maxLevel = 10;
    
    protected int parentPos = 0; // auxiliar to place block

    public bool AllowOnOff;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
            bool debug = false;
            if (this.Properties.Values.ContainsKey("debug"))
            {
                if (bool.TryParse(this.Properties.Values["debug"], out debug) == false) debug = false;
            }
            if (debug)
            {
                str = "VALVE: " + str;
                // Check if the game instance is not null
                if (GameManager.Instance != null)
                {
                    // Display the string in the chat text area
                    EntityAlive entity = GameManager.Instance.World.GetLocalPlayer();
                    GameManager.Instance.GameMessage(EnumGameMessages.Chat, str, entity);
                }
            }
        }
    }

    /// <summary>
    /// Displays tooltip text at the bottom of the screen above the tool belt
    /// </summary>
    /// <param name="str">The string to display as a tool tip</param>
    private void DisplayToolTipText(string str)
    {
        // We can only call this code once every 5 seconds because the CanPlaceBlockAt code
        // is a bit spammy (right clicking to place a block once can result in many calls)

        // Check if we are already displaying as tool tip message
        if (DateTime.Now > dteNextToolTipDisplayTime)
        {
            // Display the string as a tool tip message
            GameManager.Instance.ShowTooltip(str);

            // Set time we can next display a tool tip message (once every 5 seconds)
            dteNextToolTipDisplayTime = DateTime.Now.AddSeconds(5);
        }
    }

    public override void Init()
    {
        AllowOnOff = false;
        if (this.Properties.Values.ContainsKey("AllowOnOff"))
        {
            if (bool.TryParse(this.Properties.Values["AllowOnOff"], out AllowOnOff) == false) AllowOnOff = false;
        }
        base.Init();
        this.CanPickup = AllowOnOff;
    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos,
        EntityAlive _entityFocusing)
    {
        string textAct = "";
        if (AllowOnOff)
        {
            if (_blockValue.meta2 > 9) textAct = "Press <E> to turn ON this valve";
            else textAct = "Press <E> to turn OFF this valve";
            if (!disableDebug)
                textAct = textAct + string.Format(" (parent = {0}, power = {1})", _blockValue.meta2,
                    _blockValue.meta);
        }
        return textAct;
    }

    public override bool OnBlockActivated(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        return false;
    }

    public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx,
        Vector3i _blockPos,
        BlockValue _blockValue, EntityAlive _player)
    {
        if (AllowOnOff)
        {
            int parentValue = _blockValue.meta2;
            string msg = "Valve was turned ";
            if (parentValue > 9)
            {
                parentValue = parentValue - 9;
                msg += " ON";
                // TODO - search for linked lights and turn them on?
            }
            else
            {
                parentValue = parentValue + 9;
                msg += " OFF";
                // set power to 0.
                _blockValue.meta = (byte) 0;
                // TODO - search for linked lights and turn them OFF?
            }
            _blockValue.meta2 = (byte) parentValue;
            _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
            DisplayToolTipText(msg);
            return true;
        }
        else return false;
    }

    public override bool CanPlaceBlockAt(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        bool result = false;
        // will check if it has any GazLine, Gas Tank or Valve as a neightbor
        // if that block is not a child of this position
        // it will store that block as parent
        parentPos = 0;
        int parentPosition = GetParent(_world, _clrIdx, _blockPos);
        if (parentPosition > 0)
        {
            parentPos = parentPosition;
            _blockValue.meta2 = (byte) parentPosition;
            // it just saves the direction of the parent
            // 1 - UP; 2 - DOWN; 3 - LEFT; 4 - RIGHT; 5 - FORWARD; 6 - BACK
            // diagonals are not alowed            
            result = base.CanPlaceBlockAt(_world, _clrIdx, _blockPos, _blockValue);
        }
        else
        {
            DisplayToolTipText("This block can only be placed next to another power line or generator");
        }
        return result;
    }

    public string GetPowerType()
    {
        string powerType = "";
        if (this.Properties.Values.ContainsKey("powerType"))
        {
            powerType = this.Properties.Values["powerType"];
        }
        return powerType;
    }

    private bool TypeCheck(string powerTypeParent)
    {
        if (GetPowerType() != powerTypeParent && powerTypeParent != "")
        {
            // the type of power does not match
            return false;
        }
        return true;
    }

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea,
        Random _rnd)
    {
        // check parent here...
        if (_bpResult.blockValue.meta2 <= 0) // no parent, but MUST have one!
        {
            int parentPosition = GetParent(_world, _bpResult.clrIdx, _bpResult.blockPos);
            _bpResult.blockValue.meta2 = (byte) parentPosition;
        }
        base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);
    }

    public override void OnBlockAdded(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockAdded(_world, _chunk, _blockPos, _blockValue);
    }

    #region Finding Gaz Tank, Boiler/Generator or another valve;

    // for a gaz tank, the machine has to "ask" for power unit
    private int FindSource(WorldBase _world, int _cIdx, BlockValue _blockValue, Vector3i _blockPos,
        Vector3i _blockPosOrgin, int level, int powerUnits, int valveNumber)
    {
        // now, it will check the parent directly... If no parent is found, the line to the origin is broken
        // and energy is NOT bidirectional - so even if there are acumulator further in line
        // power for this object is null
        // should be fast to calculate like this
        int result = 0;
        if (_blockValue.meta2 > 0)
        {
            // check if parent exists. If it does, follows that line to the source
            // 1 - UP; 2 - DOWN; 3 - LEFT; 4 - RIGHT; 5 - FORWARD; 6 - BACK
            Vector3i posCheck = _blockPos;
            if (_blockValue.meta2 == 1)
                posCheck = new Vector3i(_blockPos.x, _blockPos.y + 1, _blockPos.z); // parent is up
            else if (_blockValue.meta2 == 2)
                posCheck = new Vector3i(_blockPos.x, _blockPos.y - 1, _blockPos.z); // parent is down
            else if (_blockValue.meta2 == 3)
                posCheck = new Vector3i(_blockPos.x - 1, _blockPos.y, _blockPos.z); // parent is left
            else if (_blockValue.meta2 == 4)
                posCheck = new Vector3i(_blockPos.x + 1, _blockPos.y, _blockPos.z); // parent is right
            else if (_blockValue.meta2 == 5)
                posCheck = new Vector3i(_blockPos.x, _blockPos.y, _blockPos.z + 1); // parent is forward
            else if (_blockValue.meta2 == 6)
                posCheck = new Vector3i(_blockPos.x, _blockPos.y, _blockPos.z - 1); // parent is back
            result = CheckSource(level, _world, _cIdx,
                posCheck, _blockPosOrgin, powerUnits, valveNumber);
        }
        else DisplayToolTipText("NO PARENT DEFINED");

        return result;
    }

    private int CheckSource(int level, WorldBase _world, int _cIdx, Vector3i _blockCheck, Vector3i _blockPosOrigin,
        int powerUnits, int valveNumber)
    {
        int result = 0;
        if (level > maxLevel)
        {
            DisplayChatAreaText(string.Format("LINE LIMIT REACHED AT ({0},{1},{2}", _blockCheck.x, _blockCheck.y,
                _blockCheck.z));
            return result;
                // it goes as far as maxLevel blocks away it stops, so you should plan carefully your lines using acumulators
        }
        Block blockAux = Block.list[_world.GetBlock(_cIdx, _blockCheck).ToItemValue().type];
        string blockname = blockAux.GetBlockName();
        if (blockAux is BlockGasTankSecure && (GetPowerType() == "Gaz" || GetPowerType() == ""))
        {
            #region Found a gaz tank;

            // as if it has power available - 5 ticks for each power unit on the source.
            BlockValue block = _world.GetBlock(_cIdx, _blockCheck);
            if (block.meta >= powerUnits)
                // no need to worry, just get that power since there is enough of it in the tank
            {
                DisplayChatAreaText(string.Format("Found a gaz tank with {0} power units left", block.meta));
                // retuns the new power value
                result = GetPowerTank(_world, _cIdx, _blockCheck, _blockPosOrigin, block, powerUnits);
            }
            else
            {
                // still needs to ask if its possible, just it case it has available fuel
                if ((blockAux is BlockGasTankSecure))
                {
                    DisplayChatAreaText(string.Format("REQUESTING MORE POWER (secure)"));
                    result = (blockAux as BlockGasTankSecure).GetPower(_world, _cIdx, _blockCheck, powerUnits);
                }
                block = _world.GetBlock(_cIdx, _blockCheck);
                if (block.meta >= powerUnits)
                {
                    DisplayChatAreaText(string.Format("New Tank powerLevel = {0}", block.meta));
                    result = GetPowerTank(_world, _cIdx, _blockCheck, _blockPosOrigin, block, powerUnits);
                }
                else
                {
                    DisplayChatAreaText(string.Format("Found a boiler with NO power units left"));
                    return 0;
                }
            }

            #endregion;
        }
        else if (blockAux is BlockBoiler && (GetPowerType() == "Boiler" || GetPowerType() == ""))
        {
            #region Found a boiler;

            // check if its burning        
            BlockValue blockAuxValue = _world.GetBlock(_cIdx, _blockCheck);
            if (BlockCampfire.IsCampfireLit(blockAuxValue))
            {
                DisplayChatAreaText(string.Format("FOUND BOILER TURNED ON"));
                // adds 5 power unit to self 
                AddPowerLevel(_world, _cIdx, _blockPosOrigin, 5);
                return 5;
            }
            else
            {
                DisplayChatAreaText(string.Format("FOUND BOILER TURNED OFF"));
                return 0; // boiler is not on
            }

            #endregion;
        }
        else if ((blockAux is BlockGenerator) && (GetPowerType() == "Electric" || GetPowerType() == ""))
        {
            // small generator WILL NOT PERMIT THE USE OF VALVES!!!
            if ((blockAux is BlockSmallGenerator))
            {
                DisplayChatAreaText(string.Format("FOUND SMALL GENERATOR - ABORT"));
                return 0;
            }            
            #region Found a generator;

            // check if its burning        
            BlockValue blockAuxValue = _world.GetBlock(_cIdx, _blockCheck);
            if (BlockGenerator.IsOn(blockAuxValue.meta2))
            {
                DisplayChatAreaText(string.Format("FOUND GENERATOR TURNED ON"));
                // adds 5 power unit to self 
                AddPowerLevel(_world, _cIdx, _blockPosOrigin, 5);
                return 5;
            }
            else
            {
                DisplayChatAreaText(string.Format("FOUND GENERATOR TURNED OFF"));
                return 0; // boiler is not on
            }

            #endregion;
        }
        else if (blockAux is BlockValve)
        {
            if (GetPowerType() != "")
            {
                // if for some reason the type does not match
                if (!TypeCheck((blockAux as BlockValve).GetPowerType())) return 0;
            }
            // asks valve for power, instead of going all the way to the gaz tank
            // asks for 5 power units only, since it's a valve
            if ((blockAux as BlockValve).GetPower(_world, _cIdx, _blockCheck, 5, valveNumber))
            {
                DisplayChatAreaText(string.Format("FOUND VALVE WITH POWER"));
                // adds 4 to self since if it consumes 1
                AddPowerLevel(_world, _cIdx, _blockPosOrigin, 5);
                return 5;
            }
            else
            {
                DisplayChatAreaText(string.Format("FOUND VALVE WITHOUT POWER"));
                return 0;
            }
        }
        else if (blockAux is BlockPowerLine)
        {
            if (GetPowerType() != "")
            {
                // if for some reason the type does not match
                if (!TypeCheck((blockAux as BlockPowerLine).GetPowerType())) return 0;
            }
            #region found line;

            // check one more level
            level = level + 1;
            // check parent of current posistion
            BlockValue currentValue = _world.GetBlock(_cIdx, _blockCheck);
            result = FindSource(_world, _cIdx, currentValue, _blockCheck, _blockPosOrigin, level, powerUnits, valveNumber);

            #endregion;
        }
        else
        {
            return 0; // no more line
        }
        return result;
    }

    private int GetPowerTank(WorldBase _world, int _cIdx, Vector3i _blockCheck, Vector3i _blockPosPrev,
        BlockValue block, int powerUnits)
    {
        int newPowerValue = block.meta;
        // removes 5 power units - ONCE it gets to 0 the generator has to check for more fuel                
        newPowerValue = newPowerValue - powerUnits;
        if (newPowerValue < 0) newPowerValue = 0;
        block.meta = (byte) newPowerValue;
        _world.SetBlockRPC(_cIdx, _blockCheck, block);
        // adds the powerunits to self and returns the new value
        return AddPowerLevel(_world, _cIdx, _blockPosPrev, powerUnits);
    }

    private int AddPowerLevel(WorldBase _world, int _cIdx, Vector3i _blockPos, int valueAdd)
    {
        Block blockAux = Block.list[_world.GetBlock(_cIdx, _blockPos).ToItemValue().type];
        string blockname = blockAux.GetBlockName();
        DisplayChatAreaText(string.Format("Adding {0} to {1}", valueAdd, blockname));

        BlockValue block = _world.GetBlock(_cIdx, _blockPos);
        int newPowerValue = block.meta;
        newPowerValue = newPowerValue + valueAdd;
        block.meta = (byte) newPowerValue;
        DisplayChatAreaText("MyNewPower = " + block.meta);
        _world.SetBlockRPC(_cIdx, _blockPos, block);
        return newPowerValue;
    }

    #endregion;

    #region Check parent;

    // make sure it doesn't come back to the same one as before
    private int GetParent(WorldBase _world, int _cIdx, Vector3i _blockPos)
    {
        int result = 0;
        // it only alows to go up, down, left, right, forward and back to facilitate navigation
        if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x, _blockPos.y + 1, _blockPos.z), 1))
            result = 1; // UP
        else if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x, _blockPos.y - 1, _blockPos.z), 2))
            result = 2; // DOWN
        else if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x - 1, _blockPos.y, _blockPos.z), 3))
            result = 3; // LEFT
        else if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x + 1, _blockPos.y, _blockPos.z), 4))
            result = 4; // RIGHT
        else if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x, _blockPos.y, _blockPos.z + 1), 5))
            result = 5; // FORWARD
        else if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x, _blockPos.y, _blockPos.z - 1),
            6)) result = 6; // BACK

        return result;
    }

    private bool CheckParentBlock(WorldBase _world, int _cIdx, Vector3i _blockCheck, int direction)
    {
        bool result = false;

        Block blockAux = Block.list[_world.GetBlock(_cIdx, _blockCheck).ToItemValue().type];
        // if block is compatible
        if (blockAux is BlockPowerLine)
        {
            if (GetPowerType() != "")
            {
                string powerTypeParent = "";
                // it's not a universal line, so it needs to do some checking
                // if the block is another powerline, it HAS to be of the same type.
                if (blockAux is BlockPowerLine)
                {
                    powerTypeParent = (blockAux as BlockPowerLine).GetPowerType();
                }
                if (GetPowerType() != powerTypeParent && powerTypeParent != "")
                {
                    // the type of power does not match
                    return false;
                }
            }
            // check if that block is not child
            BlockValue blkValue = _world.GetBlock(_cIdx, _blockCheck);
            int directionParent = blkValue.meta2;
            // check if I don't create a loop in line
            // for example if this wants to go up, the one up must not want to down
            if ((direction == 1 && directionParent != 2) || (direction == 2 && directionParent != 1) ||
                (direction == 3 && directionParent != 4) || (direction == 4 && directionParent != 3) ||
                (direction == 5 && directionParent != 6) || (direction == 6 && directionParent != 5))
            {
                // no loop, can place
                // can use this direction as parent
                result = true;
            }
        }
        return result;
    }

    #endregion;

    /// <summary>
    /// Asks for powerUnits
    /// </summary>
    /// <param name="_world">Worldbase</param>
    /// <param name="_cIdx">Chunk ID</param>
    /// <param name="_blockPos">Block Position</param>
    /// <param name="powerUnits">Number of Power Units requested</param>
    /// <returns>True if power units available</returns>
    public bool GetPower(WorldBase _world, int _cIdx, Vector3i _blockPos, int powerUnits, int valveNumber)
    {
        valveNumber--;
        if (valveNumber <= 0) return false;
        // checks if self has enough power units left
        // get self blockvalue
        BlockValue blkValue = _world.GetBlock(_cIdx, _blockPos);
        if (blkValue.meta2 < 9)
        {
            int newPowerValue = blkValue.meta;
            if (newPowerValue >= powerUnits)
            {
                DisplayChatAreaText("VALVE has enough power, no need to request");
                // remove the requested power units and returns true            
                newPowerValue = newPowerValue - powerUnits;
                blkValue.meta = (byte) newPowerValue;
                _world.SetBlockRPC(_cIdx, _blockPos, blkValue);
                return true;
            }
            else
            {
                // issue a request of 1 batch of power units to the master
                // 1 valve batch is ALWAYS bigger then 1 machine request
                DisplayChatAreaText("VALVE needs to ask for power to master");
                // always asks for a batch of 10 powerunits, but can get less then that
                int powerReceived = FindSource(_world, _cIdx, blkValue, _blockPos, _blockPos, 1, 10, valveNumber);
                if (newPowerValue < 0) newPowerValue = 0;
                newPowerValue = newPowerValue + powerReceived;
                if (newPowerValue >= powerUnits)
                {
                    DisplayChatAreaText("VALVE has gotten enough power");
                    // remove the requested power units and returns true            
                    newPowerValue = newPowerValue - powerUnits;
                    blkValue.meta = (byte) newPowerValue;
                    _world.SetBlockRPC(_cIdx, _blockPos, blkValue);
                    return true;
                }
                else return false;
            }
        }
        else
        {
            DisplayChatAreaText("VALVE is turned OFF");
            return false;
        }
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos,
        BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        // the animations need to be triggered here so that they are shown to all players
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        if (((int) _oldBlockValue.meta2 < 9 && (int) _newBlockValue.meta2 >= 9) ||
            ((int) _oldBlockValue.meta2 >= 9 && (int) _newBlockValue.meta2 < 9))
            // trigger animation
            playAnimation(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
    }
    private void playAnimation(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _blockValue)
    {
        BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
        Animator[] componentsInChildren;
        if (_ebcd == null || !_ebcd.bHasTransform ||
            (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Animator>(false)) == null)
            return;
        DisplayChatAreaText("FOUND ANIMATOR");
        foreach (Animator animator in componentsInChildren)
        {
            if (((int)_oldBlockValue.meta2 >= 9 && (int)_blockValue.meta2 < 9))
            {
                // play open animation
                animator.ResetTrigger("offTrigger");
                animator.SetTrigger("onTrigger");
            }
            else if (((int)_oldBlockValue.meta2 < 9 && (int)_blockValue.meta2 >= 9))
            {
                // play close animation
                animator.ResetTrigger("onTrigger");
                animator.SetTrigger("offTrigger");
            }
        }
    }

    public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
    {
        Animator[] componentsInChildren;
        if (_ebcd == null || !_ebcd.bHasTransform ||
            (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Animator>(false)) == null)
            return;
        bool _isOn = ((int)_blockValue.meta2 < 9);
        foreach (Animator animator in componentsInChildren)
        {
            if (!_isOn)
                animator.CrossFade("off", 0.0f);
            else animator.CrossFade("on", 0.0f);
        }
    }
}

/// <summary>
/// Class for power lines - transport energy - branches are 10 blocks max.
/// More then that you need to use valves
/// They can be locked to a specific source type (Generator, Boiler, GasTank
/// Mortelentus - 2016
/// </summary>
public class BlockPowerLine : Block
{
    private bool disableDebug = true;

    protected int parentPos = 0; // auxiliar to place block
    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
            // Check if the game instance is not null
            if (GameManager.Instance != null)
            {
                // Display the string in the chat text area
                EntityAlive entity = GameManager.Instance.World.GetLocalPlayer();
                GameManager.Instance.GameMessage(EnumGameMessages.Chat, str, entity);
            }
        }
    }

    /// <summary>
    /// Displays tooltip text at the bottom of the screen above the tool belt
    /// </summary>
    /// <param name="str">The string to display as a tool tip</param>
    private void DisplayToolTipText(string str)
    {
        // We can only call this code once every 5 seconds because the CanPlaceBlockAt code
        // is a bit spammy (right clicking to place a block once can result in many calls)

        // Check if we are already displaying as tool tip message
        if (DateTime.Now > dteNextToolTipDisplayTime)
        {
            // Display the string as a tool tip message
            GameManager.Instance.ShowTooltip(str);

            // Set time we can next display a tool tip message (once every 5 seconds)
            dteNextToolTipDisplayTime = DateTime.Now.AddSeconds(5);
        }
    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos,
        EntityAlive _entityFocusing)
    {
        if (!disableDebug)
            return string.Format("parent is {0}", _blockValue.meta2);
        else return "";
    }

    public override bool CanPlaceBlockAt(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        bool result = false;
        // will check if it has any GazLine, Gaz Tank or Valve as a neightbor
        // if that block is not a child of this position
        // it will store that block as parent
        parentPos = 0;
        int parentPosition = GetParent(_world, _clrIdx, _blockPos);
        if (parentPosition > 0)
        {
            parentPos = parentPosition;
            _blockValue.meta2 = (byte)parentPosition;
            // it just saves the direction of the parent
            // 1 - UP; 2 - DOWN; 3 - LEFT; 4 - RIGHT; 5 - FORWARD; 6 - BACK
            // diagonals are not alowed 
            result = base.CanPlaceBlockAt(_world, _clrIdx, _blockPos, _blockValue);
        }
        else
        {
            DisplayToolTipText("This block can only be placed next to another power line or generator");
        }
        return result;       
    }

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea, Random _rnd)
    {
        // check parent here...
        if (_bpResult.blockValue.meta2 <= 0) // no parent, but MUST have one!
        {
            int parentPosition = GetParent(_world, _bpResult.clrIdx, _bpResult.blockPos);
            _bpResult.blockValue.meta2 = (byte)parentPosition;
        }
        base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);
    }

    public string GetPowerType()
    {
        string powerType = "";
        if (this.Properties.Values.ContainsKey("powerType"))
        {
            powerType = this.Properties.Values["powerType"];
        }
        return powerType;
    }

    // make sure it doesn't come back to the same one as before
    private int GetParent(WorldBase _world, int _cIdx, Vector3i _blockPos)
    {
        int result = 0;
        // it only alows to go up, down, left, right, forward and back to facilitate navigation
        if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x, _blockPos.y + 1, _blockPos.z), 1)) result = 1; // UP
        else if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x, _blockPos.y - 1, _blockPos.z), 2)) result = 2; // DOWN
        else if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x - 1, _blockPos.y, _blockPos.z), 3)) result = 3; // LEFT
        else if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x + 1, _blockPos.y, _blockPos.z), 4)) result = 4; // RIGHT
        else if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x, _blockPos.y, _blockPos.z + 1), 5)) result = 5; // FORWARD
        else if (CheckParentBlock(_world, _cIdx, new Vector3i(_blockPos.x, _blockPos.y, _blockPos.z - 1), 6)) result = 6; // BACK

        return result;
    }

    private bool CheckParentBlock(WorldBase _world, int _cIdx, Vector3i _blockCheck, int direction)
    {
        bool result = false;

        Block blockAux = Block.list[_world.GetBlock(_cIdx, _blockCheck).ToItemValue().type];
        // if block is compatible
        if (blockAux is BlockBoiler || blockAux is BlockPowerLine || blockAux is BlockValve || blockAux is BlockGenerator || blockAux is BlockGasTankSecure)
        {            
            if (GetPowerType() != "")
            {
                string powerTypeParent = "";
                // it's not a universal line, so it needs to do some checking
                // if the block is another powerline, it HAS to be of the same type.
                if (blockAux is BlockPowerLine)
                {
                    powerTypeParent = (blockAux as BlockPowerLine).GetPowerType();                    
                }
                // if the block is a valve, it HAS to have the same source LOCK.
                else if (blockAux is BlockValve)
                {
                    powerTypeParent = (blockAux as BlockValve).GetPowerType();
                }
                else if (blockAux is BlockBoiler) powerTypeParent = "Boiler";
                else if (blockAux is BlockGasTankSecure) powerTypeParent = "Gaz";
                else if (blockAux is BlockGenerator) powerTypeParent = "Electric";


                if (GetPowerType() != powerTypeParent && powerTypeParent != "")
                {
                    // the type of power does not match
                    return false;
                }
            }
            // check if that block is not child
            BlockValue blkValue = _world.GetBlock(_cIdx, _blockCheck);
            int directionParent = blkValue.meta2;
            
            // check if I don't create a loop in line
            // for example if this wants to go up, the one up must not want to down
            if ((direction == 1 && directionParent != 2) || (direction == 2 && directionParent != 1) ||
                (direction == 3 && directionParent != 4) || (direction == 4 && directionParent != 3) ||
                (direction == 5 && directionParent != 6) || (direction == 6 && directionParent != 5))
            {
                // no loop, can place
                // can use this direction as parent
                result = true;
            }
        }
        return result;
    }

}
