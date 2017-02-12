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
public class BlockSpawner : BlockLoot
{
    private bool disableDebug = true;
    SpawnerMScript script;
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
            str = "SPAWNER: " + str;
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
        if (Stopped(_newBlockValue.meta2) && !GameManager.IsDedicatedServer)
        {
            BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
            CheckParticles(_ebcd);
        }
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
                if (_ebcd != null)
                {
                    string pos = "0";
                    try
                    {
                        pos = "1";
                        if (_ebcd.transform != null)
                        {
                            gameObject = _ebcd.transform.gameObject;
                            // adds the script if still not existing.
                            if (gameObject != null)
                            {
                                pos = "2";
                                script = gameObject.GetComponent<SpawnerMScript>();
                                pos = "3";
                                if (script == null)
                                {
                                    if (!disableDebug) Debug.Log("SPAWNER: ADDING SCRIPT");
                                    pos = "3.1";
                                    script = gameObject.AddComponent<SpawnerMScript>();
                                    pos = "3.2";
                                    script.initialize(_world, _blockPos, _clrIdx);
                                    pos = "3.3";
                                }
                                else if (!disableDebug)
                                    Debug.Log("SPAWNER: OnBlockValueChanged - SCRIPT ALREADY EXISTING AND RUNNING?");
                            }
                        }
                        pos = "4";
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(string.Format("SPAWNER: Error OnBlockValueChanged ({1}) - {0}", ex.Message, pos));
                    }
                }
            }
            //else if (!disableDebug) Debug.Log("SPAWNER: no need to add script");
        }
    }

    public override void OnBlockRemoved(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        try
        {
            BlockEntityData _ebcd = world.ChunkClusters[_chunk.ClrIdx].GetBlockEntity(_blockPos);
            script = gameObject.GetComponent<SpawnerMScript>();
            gameObject = _ebcd.transform.gameObject;
            if (script != null) script.KillScript();
        }
        catch (Exception)
        {

        }
        base.OnBlockRemoved(world, _chunk, _blockPos, _blockValue);
    }

    public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
    {
        base.ForceAnimationState(_blockValue, _ebcd);
        if (Stopped(_blockValue.meta2) && !GameManager.IsDedicatedServer)
        {
            CheckParticles(_ebcd);
        }
    }

    private bool AddScript(byte _metadata)
    {
        return ((int)_metadata & 1 << 0) != 0;
    }

    private bool Stopped(byte _metadata)
    {
        return ((int)_metadata & 1 << 1) != 0;
    }

    private bool WasLooted(byte _metadata)
    {
        return ((int)_metadata & 1 << 2) != 0;
    }

    private void CheckParticles(BlockEntityData _ebcd)
    {
        string particleAction = "None"; // this buffs are applied as soon as you get in the area.
        if (this.Properties.Values.ContainsKey("ParticleAction"))
        {
            particleAction = this.Properties.Values["ParticleAction"];
        }
        if (particleAction == "None") return;
        bool setState = false;
        if (particleAction == "Start") setState = true;
        Transform[] componentsInChildren;
        if (_ebcd == null || !_ebcd.bHasTransform ||
            (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Transform>(true)) == null)
            return;
        foreach (Transform tra in componentsInChildren)
        {
            if (tra.name == "particles")
            {
                tra.gameObject.SetActive(setState);
                break;
            }
        }
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx,
        BlockValue _blockValue, BlockEntityData _ebcd)
    {
        //Debug.Log("SPAWNER: OnBlockEntityTransformAfterActivated");
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        // force a blockvalue change to add the script.
        _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);


        // every time this happens?
        // set bit to 1
        // do this everytime.
        //if (!AddScript(_blockValue.meta2))
        //{
        //    _blockValue.meta2 = (byte)(_blockValue.meta2 | (1 << 0));
        //    _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
        //}
    }

    private byte NM([In] BlockValue obj0, [In] BlockValue obj1)
    {
        if (obj0.type == obj1.type)
            return obj0.rotation;
        Block block1 = Block.list[obj0.type];
        Block block2 = Block.list[obj1.type];
        if (block1.shape.GetType().Name == block2.shape.GetType().Name)
            return obj0.rotation;
        if (block1.shape is BlockShapeExt3dModel && block2.shape is BlockShapeNew && (int)obj0.rotation == 9)
            return (byte)8;
        if (block1.shape is BlockShapeNew && block2.shape is BlockShapeExt3dModel && (int)obj0.rotation == 8)
            return (byte)9;
        return obj0.rotation;
    }

    public override int OnBlockDamaged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _damagePoints,
        int _entityIdThatDamaged, bool _bUseHarvestTool)
    {
        ChunkCluster chunkCluster = _world.ChunkClusters[_clrIdx];
        if (chunkCluster == null)
            return 0;
        if (this.isMultiBlock && _blockValue.ischild)
        {
            Vector3i parentPos = this.multiBlockPos.GetParentPos(_blockPos, _blockValue);
            BlockValue block = chunkCluster.GetBlock(parentPos);
            if (!block.ischild)
                return Block.list[block.type].OnBlockDamaged(_world, _clrIdx, parentPos, block, _damagePoints, _entityIdThatDamaged, false);
            Debug.Log("Block on position " + (object)parentPos + " with value " + (string)(object)block + " should be a parent but is not! (6)");
            return 0;
        }
        int num1 = _blockValue.damage;
        bool flag = num1 >= Block.list[_blockValue.type].MaxDamage;
        int num2 = num1 + _damagePoints;
        if (!flag && num2 >= Block.list[_blockValue.type].MaxDamage)
        {
            chunkCluster.InvokeOnBlockDamagedDelegates(_blockPos, _blockValue, _damagePoints, _entityIdThatDamaged);
            // if the block is already "stopped", it will just completely destroy it
            // if the block is "running", it will mark it as accessible to get the loot
            if (!Stopped(_blockValue.meta2))
            {
                _blockValue.damage = 0;
                _blockValue.meta2 = (byte)(_blockValue.meta2 | (1 << 1));
                // mark it as stopped so that it is now accessible
                _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                return 0;
            }
            if (this.OnBlockDestroyedBy(_world, _clrIdx, _blockPos, _blockValue, _entityIdThatDamaged, _bUseHarvestTool))
            {
                this.SpawnDestroyParticleEffect(_world, _blockValue, _blockPos, _world.GetLightBrightness(_blockPos + new Vector3i(0, 1, 0)), this.GetColorForSide(_blockValue, BlockFace.Top), _entityIdThatDamaged);
                if (this.DowngradeBlock.type != 0)
                {
                    BlockValue _blockValue1 = this.DowngradeBlock;
                    _blockValue1.rotation = _blockValue.rotation;
                    _blockValue1.meta = _blockValue.meta;
                    if (!Block.list[_blockValue1.type].shape.IsTerrain())
                        _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue1);
                    else
                        _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue1, Block.list[_blockValue1.type].Density);
                }
                else
                    _world.SetBlockRPC(_clrIdx, _blockPos, BlockValue.Air);
            }
            return Block.list[_blockValue.type].MaxDamage;
        }
        // goes do base stuff
        return base.OnBlockDamaged(_world, _clrIdx, _blockPos, _blockValue, _damagePoints, _entityIdThatDamaged,
            _bUseHarvestTool);
    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        if (WasLooted(_blockValue.meta2)) return string.Empty;
        bool lootable = false;
        if (this.Properties.Values.ContainsKey("Lootable"))
        {
            if (bool.TryParse(this.Properties.Values["Lootable"], out lootable) == false) lootable = false;
        }
        if (!lootable) return string.Empty;
        //if (!lootable) return "You cannot loot this...";
        if (WasLooted(_blockValue.meta2))
            return "I'm Sorry, but this was already scavenged by someone...";
        if (!Stopped(_blockValue.meta2)) return "It's locked!";
        return base.GetActivationText(_world, _blockValue, _clrIdx, _blockPos, _entityFocusing);
    }

    public override bool OnBlockActivated(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        //return OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
        if (!Stopped(_blockValue.meta2))
        {
            if (!disableDebug) Debug.Log("SPAWNER: IT'S STILL RUNNING");
            return false;
        }
        if (WasLooted(_blockValue.meta2))
        {
            if (!disableDebug) Debug.Log("SPAWNER: WAS ALREADY LOOTED");
            return false;
        }
        bool lootable = false;
        if (this.Properties.Values.ContainsKey("Lootable"))
        {
            if (bool.TryParse(this.Properties.Values["Lootable"], out lootable) == false) lootable = false;
        }
        if (!lootable) return false;
        // set looted bit to 1, so that this can't be looted ever again
        _blockValue.meta2 = (byte)(_blockValue.meta2 | (1 << 2));
        _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
        return base.OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
    }
}

public class SpawnerMScript : MonoBehaviour
{
    private SpawnerMScript script;
    List<MultiBuffClassAction> BuffActions;
    private WorldBase world;
    private Vector3i blockPos;
    BlockValue blockValue = BlockValue.Air;
    private int cIdx;
    int checkRadius = 0;
    int numberToSpawn = 0;
    int maxSpawn = 0;
    string entityGroup = "";
    int spawnArea = 10;
    int radiationArea = 0;
    int checkArea = 10;
    int radiationDamage = 0;
    string buffs = "";
    ulong tickRate = 10; //in seconds
    int pauseTime = 0; // in minutes
    private int numberToPause = 0;
    DateTime dtaNextTick = DateTime.MinValue;
    DateTime dtaNextSpawn = DateTime.MinValue;
    DateTime dtaNextBuff = DateTime.MinValue;
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
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("SpawnRadius"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["SpawnRadius"], out checkRadius) == false) checkRadius = 0;
        }
        // max number of zombies it will spawn each spawning tick until max number is reached
        numberToSpawn = 0;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("NumberToSpawn"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["NumberToSpawn"], out numberToSpawn) == false)
                numberToSpawn = 0;
        }
        // max number of zombies allowed inside the check area... It will not spawn again, if this number is reached
        maxSpawn = 0;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("MaxSpawned"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["MaxSpawned"], out maxSpawn) == false) maxSpawn = 0;
        }
        // entity group used to choose the entity to spawn
        entityGroup = "";
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("EntityGroup"))
        {
            entityGroup = Block.list[blockValue.type].Properties.Values["EntityGroup"];
        }
        // the area inside wich the zombies will spawn randomly
        spawnArea = 10;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("SpawnArea"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["SpawnArea"], out spawnArea) == false) spawnArea = 10;
        }
        // the area inside wich there is radiation area and buffs apply
        radiationArea = 0;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("RadiationArea"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["RadiationArea"], out radiationArea) == false)
                radiationArea = 0;
        }
        // this is the area where the spawner looks for max number
        checkArea = 10;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("CheckArea"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["CheckArea"], out checkArea) == false) checkArea = 10;
        }
        // if pause time >= 999 the spawner will just stop after NumberToPause will met, if it is EVER met.
        // this WILL NOT STOP RADIATION DAMAGE AND BUFFS!
        pauseTime = 0; 
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("PauseTime"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["PauseTime"], out pauseTime) == false) pauseTime = 0;
        }
        numberToPause = 0;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("NumberToPause"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["NumberToPause"], out numberToPause) == false) numberToPause = 0;
        }
        #region Damage Properties;
        radiationDamage = 0;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("RadiationDamage"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["RadiationDamage"], out radiationDamage) == false) radiationDamage = 0;
        }
        buffs = ""; // this buffs are applied as soon as you get in the area.
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("Buffs"))
        {
            buffs = Block.list[blockValue.type].Properties.Values["Buffs"];
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
                Debug.Log("SPAWNER - Invalid buffs in spawner configuration: " + ex.Message);
            }            
        }

        #endregion;
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
                bool spawnNeeded = false;
                int numberSpawned = 0;
                bool killNeeded = false; // not used yet - let the game despawn them
                // spawned entities
                //SortedDictionary<int, Entity> spawnedEntities = new SortedDictionary<int, Entity>();       
                if (checkRadius > 0 && (numberToSpawn > 0 || radiationArea > 0) && maxSpawn > 0 && checkArea > 0)
                {
                    foreach (Entity entity in GameManager.Instance.World.Entities.list)
                    {
                        if (entity is EntityPlayer && entity.IsAlive())
                        {
                            try
                            {
                                float distance = Math.Abs(Vector3.Distance(entity.position, blockPos.ToVector3()));
                                if (distance <= checkRadius && numberToSpawn > 0 && !Stopped(blockValue.meta2))
                                {
                                    spawnNeeded = true;
                                }
                                if (distance <= radiationArea && radiationArea > 0)
                                    doRadiationDamage(entity as EntityPlayer);
                            }
                            catch (Exception)
                            {
                                Debug.Log("SpawnerScript: Couldnt spawn the zeds");
                            }
                        }
                        else if (entity is EntityEnemy && entity.IsAlive() && !Stopped(blockValue.meta2))
                        {
                            float distance = Math.Abs(Vector3.Distance(entity.position, blockPos.ToVector3()));
                            //if (distance <= checkRadius)
                            if (distance <= checkArea)
                            {
                                numberSpawned++;
                            }
                        }
                    }
                }
                if (DateTime.Now > dtaNextSpawn)
                {
                    dtaNextSpawn = dtaNextTick;
                    if (spawnNeeded && numberSpawned < maxSpawn)
                    {
                        if (debug) Debug.Log("Trying to spawn");
                        // spawn zeds.
                        int x;
                        int y;
                        int z;
                        // adjust number to spawn, to go as high as maxspawn
                        if ((maxSpawn - numberSpawned) < numberToSpawn) numberToSpawn = (maxSpawn - numberSpawned);
                        for (int i = 0; i < numberToSpawn; ++i)
                        {
                            GameManager.Instance.World.FindRandomSpawnPointNearPosition(blockPos.ToVector3(), 15, out x,
                                out y,
                                out z, new Vector3(spawnArea, spawnArea, spawnArea), true, false);
                            int entityID = EntityGroups.GetRandomFromGroup(entityGroup);
                            Entity spawnEntity = EntityFactory.CreateEntity(entityID,
                                new Vector3((float) x, (float) y, (float) z));
                            spawnEntity.SetSpawnerSource(EnumSpawnerSource.StaticSpawner, cIdx, entityGroup);
                            GameManager.Instance.World.SpawnEntityInWorld(spawnEntity);
                            spawnEntity = null;
                            if (numberToPause > 0) numberToPause--;
                            //if (debug) Debug.Log(string.Format("SpawnerScript: numberToPause = {0}", numberToPause));
                        }
                    }
                    else if (debug)
                    {
                        if (numberSpawned >= maxSpawn)
                            Debug.Log(string.Format("SpawnerScript: There are more then {0} zeds around...", maxSpawn));
                    }
                    if (pauseTime > 0 && numberToPause <= 0)
                    {
                        if (Block.list[blockValue.type].Properties.Values.ContainsKey("NumberToPause"))
                        {
                            if (int.TryParse(Block.list[blockValue.type].Properties.Values["NumberToPause"], out numberToPause) == false) numberToPause = 0;
                        }
                        dtaNextSpawn = DateTime.Now.AddMinutes(pauseTime);
                        if (debug) Debug.Log(string.Format("SpawnerScript: Spawn Paused until {0}", dtaNextSpawn.ToString("yyyy-MM-dd HH:mm:ss")));
                        // pause spawning, until timer ends or until the script restarts
                        if (pauseTime >= 999)
                        {
                            if (debug) Debug.Log(string.Format("SpawnerScript: Spawn STOPPED"));
                            StopSpawner();
                        }
                    }
                }
                alzheimer();
            }
        }
    }

    private bool Stopped(byte _metadata)
    {
        return ((int)_metadata & 1 << 1) != 0;
    }

    private void StopSpawner()
    {
        blockValue.meta2 = (byte)(blockValue.meta2 | (1 << 1));
        world.SetBlockRPC(cIdx, blockPos, blockValue);
    }

    private void doRadiationDamage(EntityPlayer player)
    {        
        if (radiationDamage > 0)
        {
            player.DamageEntity(DamageSource.radiation, radiationDamage, false, 1f);
        }
        if (DateTime.Now >= dtaNextBuff)
        {
            // buffs always have a delay of 10s between them
            dtaNextBuff = DateTime.Now.AddSeconds(10);
            if (this.BuffActions != null)
            {
                if (BuffActions.Count > 0)
                {
                    using (List<MultiBuffClassAction>.Enumerator enumerator = BuffActions.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                            enumerator.Current.Execute(blockValue.type, (EntityAlive) player, false,
                                EnumBodyPartHit.None, (string) null);
                    }
                }
            }
        }
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
        script = gameObject.GetComponent<SpawnerMScript>();
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