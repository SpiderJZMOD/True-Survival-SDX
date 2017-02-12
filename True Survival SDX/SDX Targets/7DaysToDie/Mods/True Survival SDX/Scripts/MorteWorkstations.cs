using System;
using UnityEngine;
using Random = System.Random;

public static class ToolCheckerFunc
{
    public static void checkTools(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        try
        {
            string toolName = "";
            // make objects visible or invisible - might not be needed to use animator, just making them active or inactive.
            TileEntityWorkstation entityWorkstation = (TileEntityWorkstation) _world.GetTileEntity(_clrIdx, _blockPos);
            if (entityWorkstation != null)
            {
                BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
                Transform[] componentsInChildren;
                if (_ebcd == null || !_ebcd.bHasTransform ||
                    (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Transform>(true)) == null)
                    return;
                //foreach (Transform tra in componentsInChildren)
                //{
                //    Debug.Log(tra.name);
                //}
                int i = 0;
                foreach (ItemStack itemS in entityWorkstation.Tools)
                {
                    toolName = string.Format("tool" + (i + 1));
                    // look for tool
                    if (!itemS.IsEmpty())
                    {
                        //Debug.Log(string.Format("The tool {0} exists", toolName));
                        // activate tool                    
                        foreach (Transform tra in componentsInChildren)
                        {
                            if (tra.name == toolName)
                            {
                                tra.gameObject.SetActive(true);
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (Transform tra in componentsInChildren)
                        {
                            if (tra.name == toolName)
                            {
                                tra.gameObject.SetActive(false);
                                break;
                            }
                        }
                    }
                    i++;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log(string.Format("ERROR CHECKTOOLS: " + ex.Message));
        }
    }
}

/// <summary>
/// workstations that need power
/// power can be: 
///      heat (generator or valve)
///      gas (tanks or valve)
///      electric (generator or valve)
/// Mortelentus - 2016
/// </summary>
public class BlockMorteToolForge : BlockForge
{
    public override ulong GetTickRate()
    {
        ulong result = 1000;
        if (this.Properties.Values.ContainsKey("TickRate"))
        {
            if (ulong.TryParse(this.Properties.Values["TickRate"], out result) == false) result = 1000;
        }
        return result;
    }

    public override void OnBlockAdded(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockAdded(world, _chunk, _blockPos, _blockValue);
        if (!world.IsRemote())
        {
            world.GetWBT().AddScheduledBlockUpdate(_chunk.ClrIdx, _blockPos, this.blockID, this.GetTickRate());
        }
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue,
        bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        // adds the next tick
        ToolCheckerFunc.checkTools(_world, _clrIdx, _blockPos, _blockValue);
        _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
        return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
    }

    public override void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);
        // every time the block is "reloaded" i try to readd it to the ticks, just in case it has stopped running
        if (!_world.IsRemote())
            _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
    }
}

public class BlockMorteToolCampfire : BlockCampfire
{
    public override ulong GetTickRate()
    {
        ulong result = 1000;
        if (this.Properties.Values.ContainsKey("TickRate"))
        {
            if (ulong.TryParse(this.Properties.Values["TickRate"], out result) == false) result = 1000;
        }
        return result;
    }

    public override void OnBlockAdded(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockAdded(world, _chunk, _blockPos, _blockValue);
        if (!world.IsRemote())
        {
            world.GetWBT().AddScheduledBlockUpdate(_chunk.ClrIdx, _blockPos, this.blockID, this.GetTickRate());
        }
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue,
        bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        // adds the next tick
        ToolCheckerFunc.checkTools(_world, _clrIdx, _blockPos, _blockValue);
        _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
        return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
    }

    public override void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);
        // every time the block is "reloaded" i try to readd it to the ticks, just in case it has stopped running
        if (!_world.IsRemote())
            _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
    }
}

public class BlockMorteToolWorkstation : BlockWorkstation
{
    public override ulong GetTickRate()
    {
        ulong result = 1000;
        if (this.Properties.Values.ContainsKey("TickRate"))
        {
            if (ulong.TryParse(this.Properties.Values["TickRate"], out result) == false) result = 1000;
        }
        return result;
    }

    public override void OnBlockAdded(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockAdded(world, _chunk, _blockPos, _blockValue);
        if (!world.IsRemote())
        {
            world.GetWBT().AddScheduledBlockUpdate(_chunk.ClrIdx, _blockPos, this.blockID, this.GetTickRate());
        }
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue,
        bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        // adds the next tick
        ToolCheckerFunc.checkTools(_world, _clrIdx, _blockPos, _blockValue);
        _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
        return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
    }

    public override void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);
        // every time the block is "reloaded" i try to readd it to the ticks, just in case it has stopped running
        if (!_world.IsRemote())
            _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
    }
}

public class BlockAssWork : BlockWorkstation
{
    public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx, Vector3i _blockPos,
        BlockValue _blockValue, EntityAlive _player)
    {
        bool result = base.OnBlockActivated(_indexInBlockActivationCommands, _world, _cIdx, _blockPos, _blockValue,
            _player);
        if (result)
        {
            LockReceips.UnlockAll();
        }
        return result;
    }
}