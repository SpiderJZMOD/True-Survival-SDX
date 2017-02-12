using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using System.Runtime.InteropServices;

/// <summary>
/// This block class works as a entity spawner
/// I've added a few extra properties:
/// Inactivity time - if no players are present, it will kill all zombies entering the area
/// Max spawn area - the zombies will spawn randomly inside that max area, that can be smaller or bigger then the trigger - useful to spread or focus the spawn
/// Radiation damage - a radiation area that can be set to inflict a configurable radiation damage. It can also apply a buff.
/// 
/// </summary>
public class BlockSleeper : BlockLoot
{
    private bool disableDebug = true;
    SleeperBlockScript script;
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
            str = "BlockSpleeper: " + str;
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
                Debug.Log(str);
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

    //spawner script only needs to run on the server
    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        if (_newBlockValue.type == BlockValue.Air.type) return;
        if (!_world.IsRemote()) // only runs on server        
        {            
            // check bit 1
            //if (AddScript(_newBlockValue.meta2) && !AddScript(_oldBlockValue.meta2))
            {
                // resets the bit
                //_newBlockValue.meta2 = (byte)(_newBlockValue.meta2 & ~(1 << 0));
                //_world.SetBlockRPC(_clrIdx, _blockPos, _newBlockValue);
                // get the transform
                BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
                if (_ebcd != null && !WasLooted(_newBlockValue.meta2))
                {
                    string pos = "0";
                    try
                    {
                        pos = "1";
                        gameObject = _ebcd.transform.gameObject;
                        // adds the script if still not existing.
                        script = gameObject.GetComponent<SleeperBlockScript>();
                        if (script == null)
                        {
                            if (!disableDebug) Debug.Log("BlockSpleeper: ADDING SCRIPT");
                            script = gameObject.AddComponent<SleeperBlockScript>();
                            script.initialize(_world, _blockPos, _clrIdx);
                        }
                        else if (!disableDebug) Debug.Log("BlockSpleeper: OnBlockValueChanged - SCRIPT ALREADY EXISTING AND RUNNING?");
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(string.Format("BlockSpleeper: Error OnBlockValueChanged ({1}) - {0}", ex.Message, pos));
                    }
                }
            }
            //else if (!disableDebug) Debug.Log("SPAWNER: no need to add script");
        }
    }

    private bool AddScript(byte _metadata)
    {
        return ((int)_metadata & 1 << 0) != 0;
    }

    private bool WasLooted(byte _metadata)
    {
        return ((int)_metadata & 1 << 2) != 0;
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx,
        BlockValue _blockValue, BlockEntityData _ebcd)
    {
        //Debug.Log("SPAWNER: OnBlockEntityTransformAfterActivated");
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        // every time this happens?
        // set bit to 1
        // add this everytime - but only if it's server
        //if (!AddScript(_blockValue.meta2))
        {
            //_blockValue.meta2 = (byte)(_blockValue.meta2 | (1 << 0));
            _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
        }
    }

    public override bool OnBlockActivated(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        // kill script
        BlockEntityData _ebcd = _world.ChunkClusters[_cIdx].GetBlockEntity(_blockPos);
        if (_ebcd != null)
        {
            try
            {
                gameObject = _ebcd.transform.gameObject;
                script = gameObject.GetComponent<SleeperBlockScript>();
                if (script!=null) script.KillScript();
            }
            catch (Exception)
            {
               
            }
        }
        // set looted bit to 1, so that it is marked to never wakeup (stops script)
        _blockValue.meta2 = (byte)(_blockValue.meta2 | (1 << 2));
        _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);        
        return base.OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
    }

    //private int GetEntityId(string eName)
    //{
    //    int entID = -1;
    //    using (Dictionary<int, EntityClass>.KeyCollection.Enumerator enumerator = EntityClass.list.Keys.GetEnumerator())
    //    {
    //        while (enumerator.MoveNext())
    //        {
    //            int current = enumerator.Current;
    //            if (EntityClass.list[current].entityClassName == eName)
    //            {
    //                entID = current;
    //                break;
    //            }
    //        }
    //    }
    //    return entID;
    //}

    public override void OnBlockRemoved(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        if (!disableDebug) Debug.Log("BLOCKSLEEPER: OnBlockRemoved");
        try
        {
            BlockEntityData _ebcd = world.ChunkClusters[_chunk.ClrIdx].GetBlockEntity(_blockPos);
            script = gameObject.GetComponent<SleeperBlockScript>();
            gameObject = _ebcd.transform.gameObject;
            if (script != null) script.KillScript();
        }
        catch (Exception)
        {
            
        }        
        //int entityID = -1;
        //string entityName = "";
        //if (Block.list[_blockValue.type].Properties.Values.ContainsKey("Entity"))
        //{
        //    entityName = Block.list[_blockValue.type].Properties.Values["Entity"];            
        //    entityID = GetEntityId(entityName);
        //    if (!disableDebug) Debug.Log("BLOCKSLEEPER: Name=" + entityName + " ID=" + entityID);
        //}
        //// spawn the entity        
        //if (entityID != 0)
        //{
        //    Entity spawnEntity = EntityFactory.CreateEntity(entityID, _blockPos.ToVector3());
        //    spawnEntity.SetSpawnerSource(EnumSpawnerSource.StaticSpawner);
        //    if (!disableDebug) Debug.Log("BLOCKSLEEPER: SPAWNING!!");
        //    GameManager.Instance.World.SpawnEntityInWorld(spawnEntity);
        //}
        base.OnBlockRemoved(world, _chunk, _blockPos, _blockValue);        
    }
}

public class SleeperBlockScript : MonoBehaviour
{
    private SleeperBlockScript script;
    List<MultiBuffClassAction> BuffActions;
    private WorldBase world;
    private Vector3i blockPos;
    BlockValue blockValue = BlockValue.Air;
    private int cIdx;
    int checkRadius = 0;
    string entityName = "";
    ulong tickRate = 10; //in seconds
    DateTime dtaNextTick = DateTime.MinValue;
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
        checkRadius = 0;
        // the area inside wich any player will trigger the spawning.
        // if all players move out of this area, spawning will stop
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("CheckArea"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["CheckArea"], out checkRadius) == false) checkRadius = 0;
        }
        // entity group used to choose the entity to spawn
        entityName = "";
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("Entity"))
        {
            entityName = Block.list[blockValue.type].Properties.Values["Entity"];
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
            if (DateTime.Now > dtaNextTick)
            {
                blockValue = world.GetBlock(cIdx, blockPos);
                dtaNextTick = DateTime.Now.AddSeconds(tickRate);
                // checks if there is any player near
                float playerModifier = 1;
                if (checkRadius > 0 && entityName != "")
                {
                    List<Entity> players =
                                    GameManager.Instance.World.GetEntitiesInBounds(typeof(EntityPlayer),
                                        new Bounds(blockPos.ToVector3(),
                                            new Vector3(checkRadius, 255, checkRadius)));

                    foreach (Entity entity in players)
                    //foreach (Entity entity in GameManager.Instance.World.Entities.list)
                    {
                        if ((entity is EntityPlayer) && entity.IsAlive())
                        {
                            try
                            {
                                //float distance = Math.Abs(Vector3.Distance(entity.position, blockPos.ToVector3()));
                                //if (debug) Debug.Log("SleeperBlockScript: DISTANCE = " + distance.ToString("#0.##"));
                                //if (distance <= checkRadius)
                                {
                                    // check if its proning to calculate chance
                                    if ((entity as EntityPlayer).Crouching)
                                        playerModifier += 0.01F; // only 1% for each player crouching, so it stays at 20%
                                    else playerModifier += 0.5F; // 50% more chance for each player not crouching
                                }
                            }
                            catch (Exception)
                            {
                                Debug.Log("SleeperBlockScript: Couldnt spawn the zeds");
                            }
                        }
                    }
                    if (playerModifier > 1)
                    {
                        if (debug) Debug.Log("SleeperBlockScript: found players!!");
                        // calculate chance
                        System.Random _rnd = new System.Random((int) (DateTime.Now.Ticks & 0x7FFFFFFF));
                        float chance = 25*playerModifier;
                        int rool = _rnd.Next(0, 100);
                        if (rool < chance)
                        {
                            if (debug) Debug.Log("SleeperBlockScript: DESTROY!");
                            // destroy block, and spawn entity at its place.
                            world.SetBlockRPC(cIdx, blockPos, BlockValue.Air);
                            int entityID = -1;
                            if (entityName != "")
                            {
                                entityID = GetEntityId(entityName);
                                if (debug) Debug.Log("BLOCKSLEEPER: Name=" + entityName + " ID=" + entityID);
                                // spawn the entity        
                                if (entityID != 0)
                                {
                                    Entity spawnEntity = EntityFactory.CreateEntity(entityID, blockPos.ToVector3());
                                    spawnEntity.SetSpawnerSource(EnumSpawnerSource.StaticSpawner);
                                    if (debug) Debug.Log("BLOCKSLEEPER: SPAWNING!!");
                                    GameManager.Instance.World.SpawnEntityInWorld(spawnEntity);
                                    KillScript();
                                }
                            }
                        }
                    }
                }

                alzheimer();
            }
        }
    }

    private int GetEntityId(string eName)
    {
        int entID = -1;
        using (Dictionary<int, EntityClass>.KeyCollection.Enumerator enumerator = EntityClass.list.Keys.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                int current = enumerator.Current;
                if (EntityClass.list[current].entityClassName == eName)
                {
                    entID = current;
                    break;
                }
            }
        }
        return entID;
    }

    private static void alzheimer()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        //SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
    }

    public void KillScript()
    {
        GameManager.Instance.windowManager.SetMouseEnabledOverride(false);
        script = gameObject.GetComponent<SleeperBlockScript>();
        if (script != null)
        {
            Destroy(this);
        }
        else
        {
            //Debug.Log("FindChildTele script not found");
        }
    }
}