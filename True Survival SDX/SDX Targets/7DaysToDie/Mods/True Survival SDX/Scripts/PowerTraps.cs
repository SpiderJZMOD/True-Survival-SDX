using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;


/// <summary>
/// Custom class for powered traps (animator control)
/// Mortelentus 2016
/// </summary>
public class BlockPowerTrap : Block
{
    private bool disableDebug = false;

    private int maxLevel = 10;
    private int valveNumber = 10;
    private int miniMumCheck = 5; // in seconds
    private int numberUnitsToRequest = 5;
    List<MultiBuffClassAction> BuffActions;

    internal int parentPos = 0; // auxiliar to place block

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    private DateTime dteNextCheckWithNoAnim;

    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
            str = "POWERTRAP: " + str;
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

    public override void OnBlockAdded(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockAdded(_world, _chunk, _blockPos, _blockValue);
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

    private void UpdateUnitsToRequest()
    {
        if (this.Properties.Values.ContainsKey("UnitsToRequest"))
        {
            if (int.TryParse(this.Properties.Values["UnitsToRequest"], out numberUnitsToRequest) == false)
                numberUnitsToRequest = 5;
        }
        else numberUnitsToRequest = 5;
    }

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea,
        Random _rnd)
    {
        // if it is triggered, no need to do anything
        DisplayChatAreaText("1");
        string powerType = GetPowerType();

        DisplayChatAreaText("PowerType = " + powerType);
        // check parent here...
        if (_bpResult.blockValue.meta2 <= 0 && powerType != "Triggered") // no parent, but MUST have one!
        {
            int parentPosition = GetParent(_world, _bpResult.clrIdx, _bpResult.blockPos);
            _bpResult.blockValue.meta2 = (byte)parentPosition;
        }
        DisplayChatAreaText("2");
        int powerLevel = _bpResult.blockValue.meta;
        DisplayChatAreaText("3");
        if (powerLevel <= 0 && powerType != "Triggered")
        {
            // request for more power here to save time on collision
            // try to get enough for 5 uses, since the device acumulates some power.
            UpdateUnitsToRequest();
            if (numberUnitsToRequest > 1)
                powerLevel = GetPower(_world, _bpResult.clrIdx, _bpResult.blockPos, numberUnitsToRequest,
                    _bpResult.blockValue);
            if (powerLevel > 0)
            {
                _bpResult.blockValue.meta = (byte)powerLevel;
            }
        }
        DisplayChatAreaText(string.Format("PowerLevel OFF = {0}, Parent at {1}", _bpResult.blockValue.meta,
            _bpResult.blockValue.meta2));
        base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);
    }

    public override bool OnEntityCollidedWithBlock(WorldBase _world, int _clrIdx, Vector3i _blockPos,
        BlockValue _blockValue, Entity _targetEntity)
    {
        bool result = false;

        if (_targetEntity.IsAlive())
        {
           // if (!_world.IsRemote())
            {
                result = playAnimation(_world, _clrIdx, _blockPos, _blockValue, _targetEntity);
                if (!result)
                {
                    // trap has no animation
                    result = doNoAnimation(_world, _clrIdx, _blockPos, _blockValue, _targetEntity);
                }
            }
        }
        if (result) return result;
        return base.OnEntityCollidedWithBlock(_world, _clrIdx, _blockPos, _blockValue, _targetEntity);
    }

    private bool doNoAnimation(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue,
        Entity _targetEntity)
    {
        if (DateTime.Now > dteNextCheckWithNoAnim)
        {            
            // checks for power, does damage 
            int powerValue = 0;
            // removes 1 level to turn on
            // when its connected to power source this value removes from "source"
            // while origin value decreases.
            powerValue = _blockValue.meta;
            DisplayChatAreaText("NO ANIMATION - CURRENT POWER = " + powerValue);
            if (powerValue <= 0)
            {
                // lucly bastard I need power - you may have time to be unharmed
                UpdateUnitsToRequest();
                powerValue = GetPower(_world, _clrIdx, _blockPos, numberUnitsToRequest, _blockValue);
                DisplayChatAreaText("NO ANIMATION - NEW POWER = " + powerValue);
            }
            if (powerValue > 0)
            {
                // passes on the new power and the parent
                powerValue = powerValue - 1;
                _blockValue.meta = (byte)powerValue;
                // if it has any damage to give does it now
                DoDamage(_blockPos, _targetEntity);
                DisplayChatAreaText("GOT POWER WITH NO ANIMATION - POWER = " + powerValue);
                _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                dteNextCheckWithNoAnim = DateTime.Now.AddSeconds(miniMumCheck);
                return true;
                // It will stay on that state for a bit, until it stops again.                
            }
        }
        else
        {
            // only does damage, needs to wait for the minimum time
            DoDamage(_blockPos, _targetEntity);
            return true;
        }
        return false;
    }

    private bool playAnimation(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, Entity _targetEntity)
    {
        BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
        Animator[] componentsInChildren;
        if (_ebcd == null || !_ebcd.bHasTransform ||
            (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Animator>(false)) == null)
            return false;
        foreach (Animator animator in componentsInChildren)
        {
            // will only play animation IF its not already animated          
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("idle"))
            {
                // play the animation on all clients                
                // checks for power, does damage 
                int powerValue = 0;
                // removes 1 level to turn on
                // when its connected to power source this value removes from "source"
                // while origin value decreases.
                powerValue = _blockValue.meta;
                if (powerValue <= 0)
                {
                    // lucly bastard I need power - you may have time to be unharmed
                    UpdateUnitsToRequest();
                    powerValue = GetPower(_world, _clrIdx, _blockPos, numberUnitsToRequest, _blockValue);
                }
                if (powerValue > 0)
                {
                    _blockValue.meta3 = 1; // triggers animation on all the clients
                    // passes on the new power and the parent
                    powerValue = powerValue - 1;
                    _blockValue.meta = (byte) powerValue;
                    // if it has any damage to give does it now
                    DoDamage(_blockPos, _targetEntity);
                    DisplayChatAreaText("TRIGGER ANIMATION");
                    animator.SetTrigger("triggerOn"); // pass this into the blockvaluechanged, so that all clients play it.
                    _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                    return true;
                    // It will stay on that state for a bit, until it stops again.                
                }
            }
            else
            {
                // found animator, but its already running
                if (_blockValue.meta3 == 1)
                {
                    _blockValue.meta3 = 0;
                    _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                }
                DoDamage(_blockPos, _targetEntity);
                return true;
            }
        }
        return false;
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        if (_newBlockValue.meta3 == 1)
        {
            // plays the animation again if not running
            playLocalAnimation(_world, _clrIdx, _blockPos, _newBlockValue);
        }
    }

    private bool playLocalAnimation(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
        Animator[] componentsInChildren;
        if (_ebcd == null || !_ebcd.bHasTransform ||
            (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Animator>(false)) == null)
            return false;
        foreach (Animator animator in componentsInChildren)
        {
            // will only play animation IF its not already animated          
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("idle"))
            {
                DisplayChatAreaText("TRIGGER ANIMATION");
                animator.SetTrigger("triggerOn"); // pass this into the blockvaluechanged, so that all clients play it.
                return true;
            }            
        }
        return false;
    }

    private bool DoDamage(Vector3i _blockPos, Entity _targetEntity)
    {
        int doDamage = 0;
        string buffs = "";
        if (this.Properties.Values.ContainsKey("Buffs"))
        {
            buffs = this.Properties.Values["Buffs"];
        }
        if (buffs != "")
        {
            try
            {
                string[] buffList = buffs.Split(',');
                if (buffList.Length > 0)
                {
                    foreach (string buffItem in buffList)
                    {
                        MultiBuffClassAction multiBuffClassAction = null;
                        multiBuffClassAction = MultiBuffClassAction.NewAction(buffItem);
                        if (this.BuffActions == null)
                            this.BuffActions = new List<MultiBuffClassAction>();
                        this.BuffActions.Add(multiBuffClassAction);
                    }
                }
                else
                {
                    MultiBuffClassAction multiBuffClassAction = null;
                    multiBuffClassAction = MultiBuffClassAction.NewAction(buffs);
                    if (this.BuffActions == null)
                        this.BuffActions = new List<MultiBuffClassAction>();
                    this.BuffActions.Add(multiBuffClassAction);
                }
            }
            catch (Exception ex)
            {
                this.BuffActions = null;
                Debug.Log("POWERTRAPS - Invalid buffs in trap configuration: " + ex.Message);
            }            
        }
        if (this.Properties.Values.ContainsKey("DamageDone"))
        {
            if (int.TryParse(this.Properties.Values["DamageDone"], out doDamage) == false) doDamage = 10;
        }
        if (doDamage == 0) return true; // no damage to do
        // check if it should attack legs
        bool hitLegs = false;
        if (this.Properties.Values.ContainsKey("HitLegs"))
        {
            if (bool.TryParse(this.Properties.Values["HitLegs"], out hitLegs) == false) hitLegs = false;
        }

        if (!(_targetEntity is EntityAlive))
            return false;
        EntityAlive entityAlive = (EntityAlive)_targetEntity;
        if (entityAlive.IsDead())
            return false;
        EnumDamageSourceType _damageSourceName = EnumDamageSourceType.BlockDamage;
        Vector3 vector3 = 6f *
                          (_targetEntity.transform.position -
                           new Vector3((float)_blockPos.x + 0.5f, (float)_blockPos.y, (float)_blockPos.z + 0.5f));

        DamageSourceEntity damageSourceEntity = new DamageSourceEntity(EnumDamageSourceType.BlockDamage, -1);
        damageSourceEntity.SetIgnoreConsecutiveDamages(true);
        bool flag = false;
        if (entityAlive is EntityZombie && !(entityAlive is EntityZombieDog) && !(entityAlive is EntityAnimalClown) && !(entityAlive is EntityAnimalHal))
        {
            if (this.BuffActions != null)
            {
                if (BuffActions.Count > 0)
                {
                    using (List<MultiBuffClassAction>.Enumerator enumerator = BuffActions.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                            enumerator.Current.Execute(this.blockID, (EntityAlive) _targetEntity, false,
                                EnumBodyPartHit.None, (string) null);
                    }
                }
            }
            if (entityAlive.IsImmuneToLegDamage || !hitLegs)
            {
                damageSourceEntity.hitTransformName = entityAlive.emodel.GetHitTransform(EnumBodyPartHit.Torso).name;
                flag = _targetEntity.DamageEntity((DamageSource) damageSourceEntity, doDamage, false, 1f) > 0;
            }
            else
            {
                int num = 5;
                if (entityAlive.bodyDamage.HasLeftLeg)
                {
                    Transform hitTransform = entityAlive.emodel.GetHitTransform(EnumBodyPartHit.LeftLowerLeg);
                    if ((UnityEngine.Object) hitTransform != (UnityEngine.Object) null)
                    {
                        damageSourceEntity.hitTransformName = hitTransform.name;
                        flag = _targetEntity.DamageEntity((DamageSource) damageSourceEntity, doDamage*num, false, 1f) >
                               0 ||
                               flag;
                        damageSourceEntity = new DamageSourceEntity(_damageSourceName, -1);
                        damageSourceEntity.SetIgnoreConsecutiveDamages(!flag);
                    }
                }
                if (entityAlive.bodyDamage.HasRightLeg)
                {
                    Transform hitTransform = entityAlive.emodel.GetHitTransform(EnumBodyPartHit.RightLowerLeg);
                    if ((UnityEngine.Object) hitTransform != (UnityEngine.Object) null)
                    {
                        damageSourceEntity.hitTransformName = hitTransform.name;
                        flag = _targetEntity.DamageEntity((DamageSource) damageSourceEntity, doDamage*num, false, 1f) >
                               0 ||
                               flag;
                    }
                }
            }
        }
        else
        {            
            flag = _targetEntity.DamageEntity((DamageSource) damageSourceEntity, doDamage, false, 1f) > 0;
        }
        if (_targetEntity.IsAlive() && (_targetEntity is EntityPlayer))
        {
            if (this.BuffActions != null)
            {
                if (BuffActions.Count > 0)
                {
                    using (List<MultiBuffClassAction>.Enumerator enumerator = BuffActions.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                            enumerator.Current.Execute(this.blockID, (EntityAlive)_targetEntity, false,
                                EnumBodyPartHit.None, (string)null);
                    }
                }
            }
        }
        return flag;
    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos,
        EntityAlive _entityFocusing)
    {
        if (!disableDebug)
            return string.Format("Parent = {0}, Power = {1}", _blockValue.meta2, _blockValue.meta);
        else return "";
        //return base.GetActivationText(_world, _blockValue, _clrIdx, _blockPos, _entityFocusing);
    }

    public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx, Vector3i _blockPos,
        BlockValue _blockValue, EntityAlive _player)
    {
        return true;
    }

    public override bool CanPlaceBlockAt(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        bool result = false;
        // will check if it has any powerline, generator or accumulator as a neightbor
        // if that block is not a child of this position
        // it will store that block as parent
        string powerType = GetPowerType();
        if (powerType != "Triggered")
        {
            parentPos = GetParent(_world, _clrIdx, _blockPos);
            if (parentPos > 0)
            {
                _blockValue.meta2 = (byte)parentPos;
                result = true;
            }
            else
            {
                DisplayToolTipText("This block can only be placed next to another power line or generator");
            }
        }
        else result = base.CanPlaceBlockAt(_world, _clrIdx, _blockPos, _blockValue);
        return result;
    }

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

    #region Finding Gaz Tank, Boiler or another valve;
    // for a gaz tank, the machine has to "ask" for power unit
    private int FindSource(WorldBase _world, int _cIdx, BlockValue _blockValue, Vector3i _blockPos, Vector3i _blockPosOrgin, int level, int powerUnits)
    {
        // now, it will check the parent directly... If no parent is found, the line to the origin is broken
        // and energy is NOT bidirectional - so even if there are acumulator further in line
        // power for this object is null
        // should be fast to calculate like this
        int result = 0;
        int parentPosition = _blockValue.meta2;
        RetryLabel:
        if (parentPosition > 0)
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
                posCheck, _blockPosOrgin, powerUnits);
        }
        else
        {
            // TODO - for now I just try to recalculate it... later I solve this. Should be ok since this is always a end of line
            parentPosition = GetParent(_world, _cIdx, _blockPosOrgin);
            if (parentPosition > 0)
            {
                _blockValue.meta2 = (byte)parentPosition;
                _world.SetBlockRPC(_cIdx, _blockPosOrgin, _blockValue);
                goto RetryLabel;
            }
            else DisplayToolTipText("NO PARENT DEFINED");
        }

        return result;
    }
    private int CheckSource(int level, WorldBase _world, int _cIdx, Vector3i _blockCheck, Vector3i _blockPosOrigin, int powerUnits)
    {
        int result = 0;
        if (level > maxLevel)
        {
            DisplayChatAreaText(string.Format("LINE LIMIT REACHED AT ({0},{1},{2}", _blockCheck.x, _blockCheck.y, _blockCheck.z));
            return result; // it goes as far as maxLevel blocks away it stops, so you should plan carefully your lines using acumulators
        }
        Block blockAux = Block.list[_world.GetBlock(_cIdx, _blockCheck).ToItemValue().type];
        string blockname = blockAux.GetBlockName();
        if (blockAux is BlockGasTankSecure && GetPowerType() == "Gaz")
        {
            #region Found a gaz tank;
            // as if it has power available - 5 ticks for each power unit on the source.
            BlockValue block = _world.GetBlock(_cIdx, _blockCheck);
            if (block.meta >= powerUnits) // no need to worry, just get that power since there is enough of it in the tank
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
        else if (blockAux is BlockBoiler && GetPowerType() == "Boiler")
        {
            #region Found a boiler;
            // check if its burning        
            BlockValue blockAuxValue = _world.GetBlock(_cIdx, _blockCheck);
            if (BlockCampfire.IsCampfireLit(blockAuxValue))
            {
                DisplayChatAreaText(string.Format("FOUND BOILER TURNED ON"));
                // adds 5 power unit to self 
                AddPowerLevel(_world, _cIdx, _blockPosOrigin, powerUnits);
                return powerUnits;
            }
            else
            {
                DisplayChatAreaText(string.Format("FOUND BOILER TURNED OFF"));
                return 0; // boiler is not on
            }

            #endregion;
        }
        else if (blockAux is BlockGenerator && GetPowerType() == "Electric")
        {
            #region Found a boiler;
            // check if its burning        
            BlockValue blockAuxValue = _world.GetBlock(_cIdx, _blockCheck);
            if (BlockGenerator.IsOn(blockAuxValue.meta2))
            {
                DisplayChatAreaText(string.Format("FOUND GENERATOR TURNED ON"));
                // adds 5 power unit to self 
                AddPowerLevel(_world, _cIdx, _blockPosOrigin, powerUnits);
                return powerUnits;
            }
            else
            {
                DisplayChatAreaText(string.Format("FOUND BOILER TURNED OFF"));
                return 0; // boiler is not on
            }

            #endregion;
        }
        else if (blockAux is BlockValve)
        {
            // needs to verify the valve powerType, to make sure.
            if (GetPowerType() != "")
            {
                if (!TypeCheck((blockAux as BlockValve).GetPowerType())) return 0;
            }
            // asks valve for power, instead of going all the way to the gaz tank
            // only asks for the requested power units only, it will not acumulate on this case
            if ((blockAux as BlockValve).GetPower(_world, _cIdx, _blockCheck, powerUnits, valveNumber))
            {
                DisplayChatAreaText(string.Format("FOUND VALVE WITH POWER"));
                // adds 4 to self since if it consumes 1
                AddPowerLevel(_world, _cIdx, _blockPosOrigin, powerUnits);
                return powerUnits;
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
            result = FindSource(_world, _cIdx, currentValue, _blockCheck, _blockPosOrigin, level, powerUnits);
            #endregion;
        }
        else
        {
            DisplayChatAreaText(string.Format("REACHED END OF LINE"));
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
        block.meta = (byte)newPowerValue;
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
        block.meta = (byte)newPowerValue;
        DisplayChatAreaText("MyNewPower = " + block.meta);
        _world.SetBlockRPC(_cIdx, _blockPos, block);
        return newPowerValue;
    }
    #endregion;

    public int GetPower(WorldBase _world, int _cIdx, Vector3i _blockPos, int powerUnits, BlockValue blkValue)
    {
        // checks if self has enough power units left
        // get self blockvalue        
        int newPowerValue = blkValue.meta;
        // issue a request of 1 batch of power units to the master
        // 1 valve batch is ALWAYS bigger then 1 machine request
        DisplayChatAreaText("asking for power to master");
        // always asks for a batch of 5 powerunits, but can get less then that
        int powerReceived = FindSource(_world, _cIdx, blkValue, _blockPos, _blockPos, 1, powerUnits);
        if (newPowerValue < 0) newPowerValue = 0;
        newPowerValue = newPowerValue + powerReceived;
        return newPowerValue;
    }
}