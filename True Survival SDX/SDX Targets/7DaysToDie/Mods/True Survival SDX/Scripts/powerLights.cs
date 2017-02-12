using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Power Lights
/// They will work only when connected to a electric source
/// Inactivity time - if no players are present, it will kill all zombies entering the area
/// Max spawn area - the zombies will spawn randomly inside that max area, that can be smaller or bigger then the trigger - useful to spread or focus the spawn
/// Radiation damage - a radiation area that can be set to inflict a configurable radiation damage. It can also apply a buff.
/// 
/// </summary>
public class BlockPowerLight : BlockLight
{
    private bool disableDebug = true;
    private int maxLevel = 10;
    private int valveNumber = 10;
    PowerLightScript script;
    UnityEngine.GameObject gameObject;

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
            str = "POWER LIGHT: " + str;
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

    //public override void OnBlockAdded(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    //{
    //    DisplayChatAreaText(string.Format("TICK random: {0}, TICK rate: {1}, BLOCKID: {2}", this.IsRandomlyTick.ToString(), this.GetTickRate(), this.blockID));       
    //    base.OnBlockAdded(world, _chunk, _blockPos, _blockValue);
    //    if (!world.IsRemote())
    //    {
    //        DisplayChatAreaText("Add next tick");
    //        world.GetWBT().AddScheduledBlockUpdate(_chunk.ClrIdx, _blockPos, this.blockID, this.GetTickRate());
    //    }
    //}

    public override ulong GetTickRate()
    {
        ulong result = 10;
        if (this.Properties.Values.ContainsKey("TickRate"))
        {
            if (ulong.TryParse(this.Properties.Values["TickRate"], out result) == false) result = 10;
        }
        return result;
    }

    //public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue,
    //    bool _bRandomTick,
    //    ulong _ticksIfLoaded, Random _rnd)
    //{
    //    CheckForPower(_world, _clrIdx, _blockPos, _blockValue);
    //    return true;
    //}

    public void CheckForPower(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        try
        {
            DisplayChatAreaText("TICK");
            bool needsAction = false;
            if (Findorigin(_world, _clrIdx, _blockValue, _blockPos, _blockPos, 1, "Electric"))
            {
                if (((int) _blockValue.meta & 2) == 0)
                {
                    // has power, turns ON the light if needed    
                    needsAction = true;
                }
            }
            else
            {
                if (((int) _blockValue.meta & 2) != 0)
                {
                    // no power, turns OFF the light if needed      
                    needsAction = true;
                }
            }
            if (needsAction)
            {
                //((World)_world).bEditorMode = true; - Does not work on dedicated server
                this.OnBlockActivated(_world, _clrIdx, _blockPos, _blockValue, null);
                //((World)_world).bEditorMode = false;
            }
            DisplayChatAreaText("Add next tick");
            _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
        }
        catch (Exception ex)
        {
            DisplayChatAreaText("Error - " + ex.Message);
            _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
        }
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
           result = CheckBoiler(level, _world, _cIdx,
                    posCheck, _blockPos);
        }
        else
        {
            // This SHOULD not happen            
            //DisplayToolTipText(string.Format("NO PARENT DEFINED FOR {0} at level {1}", blockname, level));
        }

        return result;
    }

    #region Check Generator;   
    private bool CheckBoiler(int level, WorldBase _world, int _cIdx, Vector3i _blockCheck, Vector3i _blockPosOrigin)
    {
        bool result = false;
        if (level > maxLevel)
        {
            DisplayChatAreaText(string.Format("LINE LIMIT REACHED AT ({0},{1},{2}", _blockCheck.x, _blockCheck.y, _blockCheck.z));
            return result; // it goes as far as maxLevel blocks away it stops, so you should plan carefully your lines using heat acumulators
        }
        string blockname = Block.list[_world.GetBlock(_cIdx, _blockCheck).ToItemValue().type].GetBlockName();
        Block blockAux = Block.list[_world.GetBlock(_cIdx, _blockCheck).ToItemValue().type];
        if (blockAux is BlockGenerator)
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
           if ((blockAux as BlockValve).GetPowerType()!="Electric") return false;

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
            if ((blockAux as BlockPowerLine).GetPowerType() != "Electric") return false;
            // check one more level
            level = level + 1;
            // check parent of current posistion
            BlockValue currentValue = _world.GetBlock(_cIdx, _blockCheck);
            result = Findorigin(_world, _cIdx, currentValue, _blockCheck, _blockPosOrigin, level, "Electric");
        }
        else
        {
            return false; // no more line
        }
        return result;
    }
    #endregion;

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
        string powerType = "Electric";
        if ((blockAux is BlockGenerator && powerType == "Electric") || blockAux is BlockPowerLine)
        {
            if (powerType != "")
            {
                // if for some reason the type does not match
                if (blockAux is BlockPowerLine)
                {
                    if ((blockAux as BlockPowerLine).GetPowerType() != "Electric") return false;
                }
                else if (blockAux is BlockValve)
                {
                    if ((blockAux as BlockValve).GetPowerType() != "Electric") return false;
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

    public override bool CanPlaceBlockAt(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        bool result = false;
        // will check if it has any powerline, generator or accumulator as a neightbor
        // if that block is not a child of this position
        // it will store that block as parent
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
            DisplayToolTipText("This block can only be placed next to an electric line or generator");
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

    public override bool OnBlockActivated(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        ChunkCluster chunkCluster = _world.ChunkClusters[_cIdx];
        if (chunkCluster == null)
            return false;
        IChunk chunkSync = chunkCluster.GetChunkSync(World.toChunkXZ(_blockPos.x), World.toChunkY(_blockPos.y), World.toChunkXZ(_blockPos.z));
        if (chunkSync == null)
            return false;
        BlockEntityData blockEntity = chunkSync.GetBlockEntity(_blockPos);
        if (blockEntity == null || !blockEntity.bHasTransform)
            return false;
        byte num = (byte)(1 - (((int)_blockValue.meta & 2) >> 1));
        _blockValue.meta &= (byte)253;
        _blockValue.meta |= (byte)((uint)num << 1);
        //playAnimation(_world, _cIdx, _blockPos, _blockValue);
        _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
        return true;
    }

    private static void playAnimation(BlockValue _blockValue, BlockEntityData blockEntity)
    {
        if (blockEntity == null || !blockEntity.bHasTransform)
            return;
        Transform transform1 = blockEntity.transform.Find("MainLight");
        if ((UnityEngine.Object) transform1 != (UnityEngine.Object) null)
        {
            LightLOD component = transform1.GetComponent<LightLOD>();
            if ((UnityEngine.Object) component != (UnityEngine.Object) null)
                component.SwitchOnOff(((int) _blockValue.meta & 2) != 0);
        }
        Transform transform2 = blockEntity.transform.Find("SeparatedLensFlare");
        if ((UnityEngine.Object) transform2 != (UnityEngine.Object) null)
        {
            LightLOD component = transform2.GetComponent<LightLOD>();
            if ((UnityEngine.Object) component != (UnityEngine.Object) null)
                component.SwitchOnOff(((int) _blockValue.meta & 2) != 0);
        }
        Transform transform3 = blockEntity.transform.Find("BulbGlow");
        if ((UnityEngine.Object) transform3 != (UnityEngine.Object) null)
        {
            MeshRenderer component = transform3.GetComponent<MeshRenderer>();
            if ((UnityEngine.Object) component != (UnityEngine.Object) null)
                component.enabled = ((int) _blockValue.meta & 2) != 0;
        }
        Transform transform4 = blockEntity.transform.Find("ExtraPointLight");
        if ((UnityEngine.Object) transform4 != (UnityEngine.Object) null)
        {
            LightLOD component = transform4.GetComponent<LightLOD>();
            if ((UnityEngine.Object) component != (UnityEngine.Object) null)
                component.SwitchOnOff(((int) _blockValue.meta & 2) != 0);
        }
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos,
        BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        // the animations need to be triggered here so that they are shown to all players
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        // checks if the script should be added
        if (!_world.IsRemote()) // only runs on server        
        {
            BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
            if (_ebcd != null)
            {
                try
                {
                    gameObject = _ebcd.transform.gameObject;
                    // adds the script if still not existing.
                    script = gameObject.GetComponent<PowerLightScript>();
                    if (script == null)
                    {
                        if (!disableDebug) Debug.Log("LIGHTS: ADDING SCRIPT");
                        script = gameObject.AddComponent<PowerLightScript>();
                        script.initialize(_world, _blockPos, _clrIdx);
                    }
                    else if (!disableDebug)
                        Debug.Log("LIGHTS: OnBlockValueChanged - SCRIPT ALREADY EXISTING AND RUNNING?");
                }
                catch (Exception ex)
                {
                    if (!disableDebug) Debug.Log("LIGHTS: Error OnBlockValueChanged - " + ex.Message);
                }
            }
        }
        if (((int)_oldBlockValue.meta & 2) == ((int)_newBlockValue.meta & 2)) return;
        // trigger light change
        ChunkCluster chunkCluster = _world.ChunkClusters[_clrIdx];
        if (chunkCluster == null)
            return;
        IChunk chunkSync = chunkCluster.GetChunkSync(World.toChunkXZ(_blockPos.x), World.toChunkY(_blockPos.y), World.toChunkXZ(_blockPos.z));
        if (chunkSync == null)
            return;
        BlockEntityData blockEntity = chunkSync.GetBlockEntity(_blockPos);
        playAnimation(_newBlockValue, blockEntity);
    }

    //public override void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    //{
    //    DisplayChatAreaText("ON BLOCK LOADED");
    //    base.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);
    //    // every time the block is "reloaded" i try to readd it to the ticks, just in case it has stopped running
    //    if (!_world.IsRemote())
    //        _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
    //}

    public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
    {
        playAnimation(_blockValue, _ebcd);
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx,
            BlockValue _blockValue, BlockEntityData _ebcd)
    {
        DisplayChatAreaText("LIGHTS: OnBlockEntityTransformAfterActivated");
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        // triggers a blockvaluechanged to add script on server?
        _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
    }
}

// checks if there is available electricity
public class PowerLightScript : MonoBehaviour
{
    private WorldBase world;
    private Vector3i blockPos;
    BlockValue blockValue = BlockValue.Air;
    private int cIdx;
    DateTime dtaNextCheck = DateTime.MinValue;
    ulong tickRate = 5; //in seconds
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
        tickRate = 5;
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
                    (Block.list[blockValue.type] as BlockPowerLight).CheckForPower(world, cIdx, blockPos, blockValue);
                }
                catch (Exception ex)
                {
                    //Debug.Log("POWERLIGHT: Error OnBlockValueChanged - " + ex.Message);
                }                
            }
        }
    }
}
