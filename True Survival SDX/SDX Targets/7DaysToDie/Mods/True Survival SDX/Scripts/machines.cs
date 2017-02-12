using System;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// workstations that need power
/// power can be: 
///      heat (generator or valve)
///      gas (tanks or valve)
///      electric (generator or valve)
/// Mortelentus - 2016
/// </summary>
public class BlockMachine : BlockWorkstation
{        
    protected int parentPos = 0; // auxiliar to place block
    private bool disableDebug = true;
    MachineScript script;
    UnityEngine.GameObject gameObject;
    private int maxLevel = 10;
    private int valveNumber = 10;

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
            str = "MACHINE: " + str;
            bool debug = false;
            if (this.Properties.Values.ContainsKey("debug"))
            {
                if (bool.TryParse(this.Properties.Values["debug"], out debug) == false) debug = false;
            }
            if (debug)
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

    public BlockMachine()
    {
        //this.IsRandomlyTick = true;
    }

    public override ulong GetTickRate()
    {
        ulong result = 1000;
        if (this.Properties.Values.ContainsKey("TickRate"))
        {
            if (ulong.TryParse(this.Properties.Values["TickRate"], out result) == false) result = 1000;
        }
        return result;
    }

    public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        // check if any block on its side is a generator AND if it's turned on
        // it checks for powerlines, generator OR valve classes
        // if it finds a generator/valve within 10 blocks, checks if it has power or can get power.
        bool canOperate = CanOperate(_world, _cIdx, _blockPos, _blockValue);

        if (canOperate)
            return base.OnBlockActivated(_indexInBlockActivationCommands, _world, _cIdx, _blockPos, _blockValue, _player);
        else
        {
            DisplayToolTipText("There's no power to operate this machine!");
            return false;
        }
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

    private bool CanOperate(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        bool canOperate = false;
        int level = 1;
        string powerType = GetPowerType();
        if (powerType == "Boiler" || powerType == "Electric")
            canOperate = Findorigin(_world, _cIdx, _blockValue, _blockPos, new Vector3i(0, 0, 0), level, powerType);
        else if (powerType == "Gaz")
        {
            DisplayChatAreaText("MyPower = " + _blockValue.meta);
            if (_blockValue.meta > 0)
            {
                DisplayChatAreaText("still have power, no need to request");
                canOperate = true;
                // removes 1 power unit to self
                AddPowerLevel(_world, _cIdx, _blockPos, -1);
            }
            else
            {
                DisplayChatAreaText("look for 'feeder'");
                canOperate = Findorigin(_world, _cIdx, _blockValue, _blockPos, _blockPos, level, powerType);
                // passes original pos as previous, to upate new power
            }
        }
        else canOperate = true; // no power required
        return canOperate;
    }

    // a boiler just needs to be turned on, there's no power consumption
    private bool Findorigin(WorldBase _world, int _cIdx, BlockValue _blockValue, Vector3i _blockPos, Vector3i _blockPosOrigin, int level, string powerType)
    {
        // now, it will check the parent directly... If no parent is found, the line to the origin is broken
        // and energy is NOT bidirectional - so even if there are acumulator further in line
        // power for this object is null
        // should be fast to calculate like this
        bool result = false;
        //RetryLabel:
        Block blockAux = Block.list[_world.GetBlock(_cIdx, _blockPos).ToItemValue().type];
        string blockname = blockAux.GetBlockName();
        DisplayChatAreaText(string.Format("CHECKING PARENT OF BLOCK {0}", blockname));
        if (_blockValue.meta2 <= 0)
        {
            // look for a suitable cable / generator and assume it as parent
            int parentPosition = GetParent(_world, _cIdx, _blockPos);
            if (parentPosition > 0)
            {
                _blockValue.meta2 = (byte) parentPosition;
                _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
            }
        }
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
            if (powerType == "Boiler" || powerType == "Electric")
                result = CheckBoiler(level, _world, _cIdx,
                    posCheck, _blockPos);
            else if (powerType == "Gaz")
                result = CheckGaz(level, _world, _cIdx,
                    posCheck, _blockPosOrigin);
        }
        else
        {
            // This SHOULD not happen            
            DisplayToolTipText(string.Format("This machine is not connected to any power source"));
        }

        return result;
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

    #region Check Boiler;   
    private bool CheckBoiler(int level, WorldBase _world,  int _cIdx, Vector3i _blockCheck, Vector3i _blockPosOrigin)
    {
        bool result = false;
        if (level > maxLevel)
        {
            DisplayChatAreaText(string.Format("LINE LIMIT REACHED AT ({0},{1},{2}", _blockCheck.x, _blockCheck.y, _blockCheck.z));
            return result; // it goes as far as maxLevel blocks away it stops, so you should plan carefully your lines using heat acumulators
        }
        string blockname = Block.list[_world.GetBlock(_cIdx, _blockCheck).ToItemValue().type].GetBlockName();
        Block blockAux = Block.list[_world.GetBlock(_cIdx, _blockCheck).ToItemValue().type];
        if (blockAux is BlockBoiler && GetPowerType() == "Boiler")
        {            
            // check if its burning        
            BlockValue blockAuxValue = _world.GetBlock(_cIdx, _blockCheck);
            if (BlockCampfire.IsCampfireLit(blockAuxValue))
            {
                DisplayChatAreaText(string.Format("FOUND BOILER TURNED ON"));
                return true;
            }
            else
            {
                DisplayChatAreaText(string.Format("FOUND BOILER TURNED OFF"));
                return false; // boiler is not on
            }
        }
        else if (blockAux is BlockGenerator && GetPowerType() == "Electric")
        {
            // check if its burning        
            BlockValue blockAuxValue = _world.GetBlock(_cIdx, _blockCheck);
            if (BlockGenerator.IsOn(blockAuxValue.meta2))
            {
                DisplayChatAreaText(string.Format("FOUND GENERATOR TURNED ON"));
                return true;
            }
            else
            {
                DisplayChatAreaText(string.Format("FOUND GENERATOR TURNED OFF"));
                return false; // boiler is not on
            }
        }
        else if (blockAux is BlockValve)
        {
            // needs to verify the valve powerType, to make sure.
            if (GetPowerType() != "")
            {
                if (!TypeCheck((blockAux as BlockValve).GetPowerType())) return false;
            }

            // asks valve for power, instead of going all the way to the generator
            if ((blockAux as BlockValve).GetPower(_world, _cIdx, _blockCheck, 1, valveNumber))
            {
                DisplayChatAreaText(string.Format("FOUND A VALVE WITH POWER"));
                return true; // available power
            }
            else
            {
                DisplayChatAreaText(string.Format("FOUND A VALVE WITHOUT POWER"));
                return false; // no power available
            }
        }        
        else if (blockAux is BlockPowerLine)
        {
            if (GetPowerType() != "")
            {
                // if for some reason the type does not match
                if (!TypeCheck((blockAux as BlockPowerLine).GetPowerType())) return false;
            }
            // check one more level
            level = level + 1;
            // check parent of current posistion
            BlockValue currentValue = _world.GetBlock(_cIdx, _blockCheck);
            result = Findorigin(_world, _cIdx, currentValue, _blockCheck, _blockPosOrigin, level, GetPowerType());
        }
        else
        {
            return false; // no more line
        }
        return result;
    }
    private bool FindParent(int level, WorldBase _world, int _cIdx, Vector3i _blockCheck, Vector3i _blockPosOrigin)
    {
        bool result = false;
        string blockname = Block.list[_world.GetBlock(_cIdx, _blockCheck).ToItemValue().type].GetBlockName();
        Block blockAux = Block.list[_world.GetBlock(_cIdx, _blockCheck).ToItemValue().type];
        if (blockAux is BlockBoiler && GetPowerType() == "Boiler")
        {
            return true;
        }
        else if (blockAux is BlockGenerator && GetPowerType() == "Electric")
        {
            return true;
        }
        else if (blockAux is BlockValve)
        {
            // needs to verify the valve powerType, to make sure.
            if (GetPowerType() != "")
            {
                if (!TypeCheck((blockAux as BlockValve).GetPowerType())) return false;
            }

            return true;
        }
        else if (blockAux is BlockPowerLine)
        {
            if (GetPowerType() != "")
            {
                // if for some reason the type does not match
                if (!TypeCheck((blockAux as BlockPowerLine).GetPowerType())) return false;
            }
            return true;
        }
        else
        {
            return false; // no more line
        }
        return result;
    }
    #endregion;
    #region Check Gaz Tank;
    // for a gaz tank, the machine has to "ask" for power unit
    private bool CheckGaz(int level, WorldBase _world, int _cIdx, Vector3i _blockCheck, Vector3i _blockPosOrigin)
    {
        bool result = false;
        if (level > maxLevel)
        {
            DisplayChatAreaText(string.Format("LINE LIMIT REACHED AT ({0},{1},{2}", _blockCheck.x, _blockCheck.y, _blockCheck.z));
            return result; // it goes as far as maxLevel blocks away it stops, so you should plan carefully your lines using acumulators
        }        
        Block blockAux = Block.list[_world.GetBlock(_cIdx, _blockCheck).ToItemValue().type];
        string blockname = blockAux.GetBlockName();
        if (blockAux is BlockGasTankSecure)
        {
            #region Found a gaz tank, check if there is any gaz left;
            // as if it has power available - 5 ticks for each power unit on the source.
            BlockValue block = _world.GetBlock(_cIdx, _blockCheck);
            if (block.meta >= 5)
            {
                DisplayChatAreaText(string.Format("Found a boiler with {0} power units left", block.meta));
                GetPowerTank(_world, _cIdx, _blockCheck, _blockPosOrigin, block);
                result = true;
            }
            else
            {
                // still needs to ask if its possible, just it case it has available fuel
                if ((blockAux is BlockGasTankSecure))
                {
                    DisplayChatAreaText(string.Format("REQUESTING MORE POWER (secure)"));
                    (blockAux as BlockGasTankSecure).GetPower(_world, _cIdx, _blockCheck, 5);                    
                }
                block = _world.GetBlock(_cIdx, _blockCheck);
                if (block.meta >= 5)
                {
                    DisplayChatAreaText(string.Format("Successfully request {0} for more power units", block.meta));
                    GetPowerTank(_world, _cIdx, _blockCheck, _blockPosOrigin, block);
                    return true;
                }
                else
                {
                    DisplayChatAreaText(string.Format("Found a boiler with NO power units left"));
                    return false;   
                }
            }
            #endregion;
        }
        else if (blockAux is BlockValve)
        {
            if (GetPowerType() != "")
            {
                // if for some reason the type does not match
                if (!TypeCheck((blockAux as BlockValve).GetPowerType())) return false;
            }
            DisplayChatAreaText("ask valve for power");
            // asks valve for power, instead of going all the way to the gaz tank
            // asks for 5 power units, since the machine itself acumulates some gaz
            if ((blockAux as BlockValve).GetPower(_world, _cIdx, _blockCheck, 5, valveNumber))
            {
                // adds 4 to self since if it consumes 1
                AddPowerLevel(_world, _cIdx, _blockPosOrigin, 4);
                return true;
            }
            else return false;
        }
        else if (blockAux is BlockPowerLine)
        {
            if (GetPowerType() != "")
            {
                // if for some reason the type does not match
                if (!TypeCheck((blockAux as BlockPowerLine).GetPowerType())) return false;
            }
            // check one more level
            level = level + 1;
            // check parent of current posistion
            BlockValue currentValue = _world.GetBlock(_cIdx, _blockCheck);
            result = Findorigin(_world, _cIdx, currentValue, _blockCheck, _blockPosOrigin, level, "Gaz");
        }
        else
        {
            return false; // no more line
        }
        return result;
    }

    private int GetPowerTank(WorldBase _world, int _cIdx, Vector3i _blockCheck, Vector3i _blockPosOrigin,
        BlockValue block)
    {
        int newPowerValue = block.meta;
        // removes 1 power unit - ONCE it gets to 0 the generator has to check for more fuel                
        newPowerValue = newPowerValue - 5;
        block.meta = (byte)newPowerValue;
        _world.SetBlockRPC(_cIdx, _blockCheck, block);
        // adds 4 power unit to self (1 is consumed with this operation)
        AddPowerLevel(_world, _cIdx, _blockPosOrigin, 4);
        return newPowerValue;
    }

    private void AddPowerLevel(WorldBase _world, int _cIdx, Vector3i _blockPos, int valueAdd)
    {
        Block blockAux = Block.list[_world.GetBlock(_cIdx, _blockPos).ToItemValue().type];
        string blockname = blockAux.GetBlockName();
        DisplayChatAreaText(string.Format("Adding {0} to {1}", valueAdd, blockname));

        BlockValue block = _world.GetBlock(_cIdx, _blockPos);
        int newPowerValue = block.meta;               
        newPowerValue = newPowerValue + valueAdd;
        block.meta = (byte)newPowerValue;
        DisplayChatAreaText("MyNewPower = " + block.meta);
        _world.SetBlockRPC(_cIdx, _blockPos, block);
    }
    #endregion;
    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos,
       EntityAlive _entityFocusing)
    {
        if (!disableDebug)
            return string.Format("Press <E> to operate this machine (parent = {0}, power = {1})", _blockValue.meta2,
                _blockValue.meta);
        else
            return string.Format("Press <E> to operate this machine");
        //return base.GetActivationText(_world, _blockValue, _clrIdx, _blockPos, _entityFocusing);
    }

    public override bool CanPlaceBlockAt(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        bool result = false;
        // will check if it has any powerline, generator or accumulator as a neightbor
        // if that block is not a child of this position
        // it will store that block as parent
        parentPos = 0;
        int parentPosition = GetParent(_world, _clrIdx, _blockPos);
        if (parentPosition > 0)
        {
            //parentPos = parentPosition;
            //_blockValue.meta2 = (byte)parentPosition;            
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
        if (_bpResult.blockValue.meta2 <= 0) // no parent, but MUST have one!
        {
            int parentPosition = GetParent(_world, _bpResult.clrIdx, _bpResult.blockPos);
            _bpResult.blockValue.meta2 = (byte)parentPosition;
            DisplayChatAreaText(string.Format("parent defined to {0}", _bpResult.blockValue.meta2));
        }
        base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);
    }

    //public override void OnBlockAdded(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    //{
    //    // set next tick
    //    if (!_world.IsRemote())
    //        _world.GetWBT().AddScheduledBlockUpdate(_chunk.ClrIdx, _blockPos, this.blockID, this.GetTickRate());
    //    base.OnBlockAdded(_world, _chunk, _blockPos, _blockValue);
    //}

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        //CheckForPower(_world, _clrIdx, _blockPos, _blockValue);
        //// adds the next tick
        //_world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
        return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
    }

    public void CheckForPower(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        DisplayChatAreaText("TICK");
        try
        {
            // check if its actually building anything
            bool checkPowerNeeded = false;
            TileEntityWorkstation entityWorkstation = (TileEntityWorkstation) _world.GetTileEntity(_clrIdx, _blockPos);
            if (entityWorkstation != null)
            {
                foreach (RecipeQueueItem itemR in entityWorkstation.Queue)
                {
                    if (itemR.IsCrafting)
                    {
                        checkPowerNeeded = true;
                        break;
                    }
                }
            }
            // check if it still has power
            if (checkPowerNeeded)
            {
                bool canOperate = CanOperate(_world, _clrIdx, _blockPos, _blockValue);
                if (canOperate) DisplayChatAreaText("TICK WITH POWER");
                else
                {
                    // remove items from input?
                    int i = 0;
                    foreach (RecipeQueueItem itemR in entityWorkstation.Queue)
                    {
                        if (itemR.IsCrafting)
                        {
                            // try fucking up the queue... using this shitty way, user looses the queued items. Completed items are kept untouched.
                            // TODO - try to find a way to return stuff to player IF he is near.
                            entityWorkstation.Queue[i] = null;
                        }
                        i++;
                    }
                    entityWorkstation.SetModified();
                    DisplayChatAreaText("TICK WITHOUT POWER");
                }
            }
        }
        catch (Exception ex)
        {
            DisplayChatAreaText(ex.ToString());
        }
    }

    //public override void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    //{
    //    DisplayChatAreaText("ON BLOCK LOADED");
    //    base.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);
    //    // every time the block is "reloaded" i try to readd it to the ticks, just in case it has stopped running
    //    if (!_world.IsRemote())
    //        _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
    //}

    #region Check Parent;
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
        string powerType = GetPowerType();
        if (blockAux is BlockValve || blockAux is BlockPowerLine || (blockAux is BlockBoiler && powerType == "Boiler") || (blockAux is BlockGasTankSecure && powerType == "Gaz") || (blockAux is BlockGenerator && powerType == "Electric"))
        {
            if (GetPowerType() != "")
            {
                // if for some reason the type does not match
                if (blockAux is BlockPowerLine)
                {
                    if (!TypeCheck((blockAux as BlockPowerLine).GetPowerType())) return false;
                }
                else if (blockAux is BlockValve)
                {
                    if (!TypeCheck((blockAux as BlockValve).GetPowerType())) return false;
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

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx,
        BlockValue _blockValue, BlockEntityData _ebcd)
    {
        DisplayChatAreaText("MACHINE: OnBlockEntityTransformAfterActivated");
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        // triggers a blockvaluechanged to add script on server?
        _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {        
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        // checks if the script should be added
        if (!_world.IsRemote()) // only runs on server        
        {
            BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
            if (_ebcd != null)
            {
                try
                {
                    if (_ebcd.transform != null)
                    {
                        gameObject = _ebcd.transform.gameObject;
                        if (gameObject != null)
                        {
                            // adds the script if still not existing.
                            script = gameObject.GetComponent<MachineScript>();
                            if (script == null)
                            {
                                if (!disableDebug) Debug.Log("MACHINE: ADDING SCRIPT");
                                script = gameObject.AddComponent<MachineScript>();
                                script.initialize(_world, _blockPos, _clrIdx);
                            }
                            else if (!disableDebug)
                                Debug.Log("MACHINE: OnBlockValueChanged - SCRIPT ALREADY EXISTING AND RUNNING?");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log("MACHINE: Error OnBlockValueChanged - " + ex.Message);
                }
            }
        }
    }
}

public class MachineScript : MonoBehaviour
{
    private WorldBase world;
    private Vector3i blockPos;
    BlockValue blockValue = BlockValue.Air;
    private int cIdx;
    DateTime dtaNextCheck = DateTime.MinValue;
    ulong tickRate = 10; //in seconds
    private bool debug = false;

    void Start()
    {

    }

    public void initialize(WorldBase _world, Vector3i _blockPos,
        int _cIdx)
    {
        blockPos = _blockPos;
        cIdx = _cIdx;
        // fill properties
        blockValue = _world.GetBlock(cIdx, blockPos);
        tickRate = 10;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("TickRate"))
        {
            if (ulong.TryParse(Block.list[blockValue.type].Properties.Values["TickRate"], out tickRate) == false) tickRate = 10;
        }
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("debug"))
        {
            if (bool.TryParse(Block.list[blockValue.type].Properties.Values["debug"], out debug) == false) debug = false;
        }
        world = _world;
    }

    void Update()
    {
        if (world != null)
        {
            if (DateTime.Now > dtaNextCheck)
            {
                dtaNextCheck = DateTime.Now.AddSeconds(tickRate);
                try
                {
                    blockValue = world.GetBlock(cIdx, blockPos);
                    (Block.list[blockValue.type] as BlockMachine).CheckForPower(world, cIdx, blockPos, blockValue);
                }
                catch (Exception ex)
                {
                    Debug.Log("MACHINE: Error OnBlockValueChanged - " + ex.Message);
                }                
            }
        }
    }
}