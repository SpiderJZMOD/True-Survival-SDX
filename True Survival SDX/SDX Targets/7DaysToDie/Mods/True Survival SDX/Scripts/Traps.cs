using System;
using System.Collections.Generic;
using UniLinq;
using UnityEngine;
using Random = System.Random;
/// <summary>
/// Custom class for unpowered traps
/// Mortelentus 2016 - v1.0
/// </summary>
public class BlockTrapMorte : Block
{
    private bool disableDebug = true;
    private string WJ;
    private string EJ;
    List<MultiBuffClassAction> BuffActions;
    List<MultiBuffClassAction> critBuffActions;

    private BlockActivationCommand[] QJ = new BlockActivationCommand[1]
    {
        new BlockActivationCommand("take", "hand", false)
    };

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
            str = "TRAPMORTE: " + str;
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
                    //GameManager.Instance.GameMessageClient(EnumGameMessages.Chat, str, "", false, "", false);
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
        base.Init();
        if (this.Properties.Values.ContainsKey("OpenSound"))
            this.WJ = this.Properties.Values["OpenSound"];
        if (this.Properties.Values.ContainsKey("CloseSound"))
            this.EJ = this.Properties.Values["CloseSound"];
        this.IsRandomlyTick = true;
    }

    public override void GetCollidingAABB(BlockValue _blockValue, int _x, int _y, int _z, float _distortedAddY, Bounds _aabb, List<Bounds> _aabbList)
    {
        base.GetCollidingAABB(_blockValue, _x, _y, _z, _distortedAddY, _aabb, _aabbList);
    }

    public override void OnEntityWalking(WorldBase _world, int _x, int _y, int _z, BlockValue _blockValue, Entity entity)
    {
        // try to activate it here too, if needed?
        //IChunk chk = _world.GetChunkFromWorldPos(new Vector3i(_x, _y, _z));        
        //chk.GetBlockEntity()
        //DisplayChatAreaText("WALK");
        //OperateTrap(_world, chk.GetHashCode(), new Vector3i(_x, _y, _z), _blockValue, entity, true);
        base.OnEntityWalking(_world, _x, _y, _z, _blockValue, entity);
    }

    public override bool OnEntityCollidedWithBlock(WorldBase _world, int _clrIdx, Vector3i _blockPos,
        BlockValue _blockValue, Entity _entity)
    {
        //DisplayChatAreaText("COLLIDE");
        OperateTrap(_world, _clrIdx, _blockPos, _blockValue, _entity, true);        
        return true;
        //return base.OnEntityCollidedWithBlock(_world, _clrIdx, _blockPos, _blockValue, _entity);
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        // the animations need to be triggered here so that they are shown to all players
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        if (!(this.shape is BlockShapeModelEntity) || _oldBlockValue.type == _newBlockValue.type && (int)_oldBlockValue.meta == (int)_newBlockValue.meta || _newBlockValue.ischild)
            return;
        // trigger animation
        playAnimation(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
    }

    private void playAnimation(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue, BlockValue _blockValue)
    {      
        BlockEntityData _ebcd = _world.ChunkClusters[_clrIdx].GetBlockEntity(_blockPos);
        Animator[] componentsInChildren;
        if (_ebcd == null || !_ebcd.bHasTransform ||
            (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Animator>(false)) == null)
            return;
        DisplayChatAreaText("FOUND ANIMATOR");
        foreach (Animator animator in componentsInChildren)
        {
            if (BlockTrapMorte.IsTrapFired(_blockValue.meta) && !BlockTrapMorte.IsTrapFired(_oldBlockValue.meta))
            {
                // play open animation
                DisplayChatAreaText("OPEN ANIMATION");
                Audio.Manager.BroadcastPlay(_blockPos.ToVector3(), WJ);
                //AudioManager.AudioManager.Play(_blockPos.ToVector3(), WJ, 0, false, -1, -1, 0F);
                animator.SetTrigger("openTrigger");
                animator.SetBool("isTriggered", true);
            }
            else if (!BlockTrapMorte.IsTrapFired(_blockValue.meta) && BlockTrapMorte.IsTrapFired(_oldBlockValue.meta))
            {
                // play close animation
                DisplayChatAreaText("CLOSE ANIMATION");
                Audio.Manager.BroadcastPlay(_blockPos.ToVector3(), EJ);
                //AudioManager.AudioManager.Play(_blockPos.ToVector3(), EJ, 0, false, -1, -1, 0F);
                animator.SetTrigger("closeTrigger");
                animator.SetBool("isTriggered", false);
            }
            else if (BlockTrapMorte.IsTrapBaited(_blockValue.meta) && !BlockTrapMorte.IsTrapBaited(_oldBlockValue.meta))
            {
                // play baited animation?
                DisplayChatAreaText("BAITE ANIMATION");
                animator.SetTrigger("baiteTrigger");
                animator.SetBool("isTriggered", false);
            }            
        }
    }

    private void OperateTrap(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, Entity _entity,
        bool fireTrap)
    {
        // aqui só vou mudar o estado do _blockvalue - no onchange é que vou fazer o trigger da animação
        // para que a mesma seja mostrada a TODOS os utiliadores na zona.
        if (!_entity.IsAlive()) return;
        DisplayChatAreaText("OPERATE");
        if (fireTrap)
        {
            if (!BlockTrapMorte.IsTrapFired(_blockValue.meta))
            {
                DisplayChatAreaText("FIRE TRAP");
                #region Fire trap;                                         
                // save info that the trap was already triggered, so that it always start on this state (force animation)
                // I will be using the bit 0.
                _blockValue.meta = (byte) (_blockValue.meta | (1 << 0));
                // On this state it's ALWAYS unbaited (bit 1)
                _blockValue.meta = (byte) (_blockValue.meta & ~(1 << 1));
                _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                // do the damage now                                        
                int animalNum = DoDamage(_blockPos, _entity);
                if (_entity.IsDead() && !(_entity is EntityPlayer))
                {
                    bool destroyOnDeath = false;
                    if (this.Properties.Values.ContainsKey("DestroyOnDeath"))
                    {
                        if (bool.TryParse(this.Properties.Values["DestroyOnDeath"], out destroyOnDeath) == false) destroyOnDeath = false;
                    }
                    // if it has a specific animal, ALWAYS destroys on death, since its capturing it.
                    if (destroyOnDeath)
                    {
                        // make the entity disapear
                        //(_entity as EntityAlive).SetDeathTime();
                        _entity.MarkToUnload();                                                
                    }
                }
                if (_entity is EntityAnimal && _entity.IsDead())
                {
                    // save information that it was an animal. The player will get meat + skin when he resets the trap. I'll use bit 2
                    _blockValue.meta = (byte) (_blockValue.meta | (1 << 2));
                    if (animalNum > 0) _blockValue.meta3 = (byte) animalNum;
                    _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                }
                #endregion;
            }
        }
        else
        {
            if (BlockTrapMorte.IsTrapFired(_blockValue.meta))
            {
                DisplayChatAreaText("RESET TRAP");

                #region Reset trap;                                 
                bool allowBaite = false;
                if (this.Properties.Values.ContainsKey("Baited"))
                {
                    if (bool.TryParse(this.Properties.Values["Baited"], out allowBaite) == false) allowBaite = false;
                }
                if (allowBaite && BlockTrapMorte.IsAnimalTriggered(_blockValue.meta) && (_entity is EntityPlayer))
                {
                    // if the trap is baited, and was triggered by an animal, give the player random amount of defined loot.                    
                    string[] animalcap = null;
                    string animAux = "";
                    char[] splitter = new char[1];
                    string[] auxStrings;
                    splitter[0] = ',';
                    if (this.Properties.Values.ContainsKey("AnimalToCatch"))
                    {
                        animAux = this.Properties.Values["AnimalToCatch"];
                        auxStrings = animAux.Split(splitter);
                        if (auxStrings.Length > 0)
                        {
                            animalcap = auxStrings;
                        }
                        else
                        {
                            animalcap = new string[1];
                            animalcap[0] = animAux;
                        }
                    }
                    string loot1 = "";
                    if (this.Properties.Values.ContainsKey("Loot1"))
                    {
                        loot1 = this.Properties.Values["Loot1"];
                    }
                    string loot2 = "";
                    if (this.Properties.Values.ContainsKey("Loot2"))
                    {
                        loot2 = this.Properties.Values["Loot2"];
                    }
                    int numLoot1 = 0;
                    int numLoot2 = 0;
                    // if it is a animal capture trap, the number will always be 1!
                    if (animAux != "")
                    {
                        if (_blockValue.meta3 == 1)
                        {
                            numLoot1 = 1;
                        }
                        else if (_blockValue.meta3 == 2)
                        {                            
                            numLoot2 = 1;
                        }

                    }
                    else
                    {
                        Random r = new Random();
                        numLoot1 = r.Next(0, 4);
                        r = new Random();
                        numLoot2 = r.Next(0, 2);
                    }                                        
                    if (numLoot1 > 0 && loot1 != "")
                    {
                        ItemValue itemLoot1 = ItemClass.GetItem(loot1);
                        if (itemLoot1 != null)
                        {
                            ItemStack loot1Stack = new ItemStack(itemLoot1, numLoot1);
                            (_entity as EntityPlayer).AddUIHarvestingItem(loot1Stack, false);
                            (_entity as EntityPlayer).bag.AddItem(loot1Stack);
                        }
                    }
                    if (numLoot2 > 0 && loot2 != "")
                    {
                        ItemValue itemLoot2 = ItemClass.GetItem(loot2);
                        if (itemLoot2 != null)
                        {
                            ItemStack loot2Stack = new ItemStack(itemLoot2, numLoot2);
                            (_entity as EntityPlayer).AddUIHarvestingItem(loot2Stack, false);
                            (_entity as EntityPlayer).bag.AddItem(loot2Stack);
                        }
                    }
                    if (numLoot2 > 0 || numLoot1 > 0) DisplayToolTipText("You got something...");
                }
                // save info that the trap is ready again, so that it always start on this state (force animation)
                // I will be using the bit 0
                _blockValue.meta = (byte) (_blockValue.meta & ~(1 << 0)); // reset triggered flag
                _blockValue.meta = (byte) (_blockValue.meta & ~(1 << 1)); // reset baited flag
                _blockValue.meta = (byte) (_blockValue.meta & ~(1 << 2)); // reset triggered by animal flag
                _blockValue.meta = (byte) (_blockValue.meta & ~(1 << 3)); // reset already spawned animal flag
                _blockValue.meta3 = 0;
                DisplayChatAreaText(string.Format("META = {0}", _blockValue.meta));
                _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);

                #endregion;
            }
            else
            {
                #region Baite trap;
                bool allowBaite = false;
                if (this.Properties.Values.ContainsKey("Baited"))
                {
                    if (bool.TryParse(this.Properties.Values["Baited"], out allowBaite) == false) allowBaite = false;
                }
                if (allowBaite && !BlockTrapMorte.IsTrapBaited(_blockValue.meta))
                {
                    // to bait trap, needs to check if the player has raw meat (rawMeat) on him.
                    if (_entity is EntityPlayer)
                    {
                        string baite = "";
                        if (this.Properties.Values.ContainsKey("Baite"))
                        {
                            baite = this.Properties.Values["Baite"];
                        }
                        ItemValue baiteItem = ItemClass.GetItem(baite);                        
                        int numMeat = (_entity as EntityPlayer).bag.GetItemCount(baiteItem);                        
                        if (numMeat >= 1)
                        {
                            (_entity as EntityPlayer).bag.DecItem(baiteItem, 1);
                            DisplayChatAreaText("BAITE TRAP");
                            //animator.SetTrigger("baiteTrigger");
                            //animator.SetBool("isTriggered", false);
                            //_entity.PlayOneShot(WJ);
                            // save info that the trap is baited
                            // I will be using the bit 1.
                            _blockValue.meta = (byte) (_blockValue.meta | (1 << 1));
                            DisplayChatAreaText(string.Format("META = {0}", _blockValue.meta));
                            _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                        }
                        else DisplayToolTipText(string.Format("You don't have enough {0} to baite this trap", ItemClass.list[baiteItem.type].localizedName));
                    }
                }
                #endregion;
            }
        }
    }

    public override bool OnBlockActivated(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {       
        OperateTrap(_world, _clrIdx, _blockPos, _blockValue, _player, false);
        return true;
    }

    public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx, Vector3i _blockPos,
        BlockValue _blockValue, EntityAlive _player)
    {
        return OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
    }

    public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
    {                
        Animator[] componentsInChildren;
        if (_ebcd == null || !_ebcd.bHasTransform ||
            (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Animator>(false)) == null)
            return;
        bool flag = BlockTrapMorte.IsTrapFired(_blockValue.meta);
        bool baited = BlockTrapMorte.IsTrapBaited(_blockValue.meta);
        DisplayChatAreaText(string.Format("TRAP triggered={0}, baited={1}", flag.ToString(), baited.ToString()));
        foreach (Animator animator in componentsInChildren)
        {
            animator.SetBool("isTriggered", flag);
            if (!flag && baited)
                animator.CrossFade("Baited", 0.0f);
            else if (flag)
                animator.CrossFade("Open", 0.0f);
            else
                animator.CrossFade("Close", 0.0f);
        }
    }

    /// <summary>
    /// Check if trap is already triggered
    /// </summary>
    /// <param name="_metadata"></param>
    /// <returns>True if triggered</returns>
    public static bool IsTrapFired(byte _metadata)
    {
        return ((int) _metadata & 1 << 0) != 0;
    }
    /// <summary>
    /// Checks if trap was triggered by an animal
    /// </summary>
    /// <param name="_metadata"></param>
    /// <returns>True if baited</returns>
    public static bool IsAnimalTriggered(byte _metadata)
    {
        return ((int)_metadata & 1 << 2) != 0;
    }
    /// <summary>
    /// Checks if trap is baited
    /// </summary>
    /// <param name="_metadata"></param>
    /// <returns>True if baited</returns>
    public static bool IsTrapBaited(byte _metadata)
    {
        return ((int)_metadata & 1 << 1) != 0;
    }
    /// <summary>
    /// Checks if trap already spawned an animal
    /// </summary>
    /// <param name="_metadata"></param>
    /// <returns>True if spawned</returns>
    public static bool IsAnimalSpawned(byte _metadata)
    {
        return ((int)_metadata & 1 << 3) != 0;
    }

    //public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    //{
    //    this.QJ[0].enabled = !BlockTrapMorte.IsTrapFired(_blockValue.meta);
    //    return this.QJ;
    //}

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos,
        EntityAlive _entityFocusing)
    {
        bool autoreset = false;
        if (this.Properties.Values.ContainsKey("AutoReset"))
        {
            if (bool.TryParse(this.Properties.Values["AutoReset"], out autoreset) == false) autoreset = false;
        }
        if (autoreset) return "";
        if (!BlockTrapMorte.IsTrapFired(_blockValue.meta))
        {
            bool allowBaite = false;
            if (this.Properties.Values.ContainsKey("Baited"))
            {
                if (bool.TryParse(this.Properties.Values["Baited"], out allowBaite) == false) allowBaite = false;
            }
            if (allowBaite && !BlockTrapMorte.IsTrapBaited(_blockValue.meta))
                return "Press <{0}> to baite trap";
            else return "";
        }
        else
        {
            if (!BlockTrapMorte.IsAnimalTriggered(_blockValue.meta))
                return "Press <{0}> to reset trap";
            else return "Press <{0}> to check and reset trap";
        }
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        DisplayChatAreaText("TICKING");
        //find nearby entities
        if (BlockTrapMorte.IsTrapBaited(_blockValue.meta))
        {
            DisplayChatAreaText("LOOK FOR ANIMALS");
            foreach (Entity entity in GameManager.Instance.World.Entities.list)
            {
                if (entity is EntityAnimal && entity.IsAlive())
                {
                    float distance = Math.Abs(Vector3.Distance(entity.position, _blockPos.ToVector3()));                    
                    if (distance <= 50)
                    {
                        DisplayChatAreaText("FOUND ALIVE ANIMAL AT DISTANCE " + distance.ToString());
                        EntityAlive ent = (entity as EntityAlive);
                        //ent.SetInvestigatePosition(_blockPos.ToVector3(), 20*60);
                        ent.SetInvestigatePosition(_blockPos.ToVector3(), 9999);
                        ent.getNavigator().clearPathEntity();
                        Legacy.PathFinderThread.Instance.FindPath(ent, _blockPos.ToVector3(), ent.GetWanderSpeed(), (EAIBase)null);
                    }
                }
            }
        }
        if (BlockTrapMorte.IsTrapBaited(_blockValue.meta) && !BlockTrapMorte.IsAnimalSpawned(_blockValue.meta))
        {
            int spawnChance = 0;
            if (this.Properties.Values.ContainsKey("SpawnChance"))
            {
                if (int.TryParse(this.Properties.Values["SpawnChance"], out spawnChance) == false) spawnChance = 10;
            }
            // use an entityGroup
            string entityGroup = "";
            if (this.Properties.Values.ContainsKey("EntityGroup"))
            {
                entityGroup = this.Properties.Values["EntityGroup"];
            }
            if (entityGroup == "")
            {
                // no need to spawn
                MarkSpawned(_world, _clrIdx, _blockPos, _blockValue);
            }
            else
            {
                Random r = new Random();
                int spawnNow = r.Next(1, 101);
                if (spawnNow <= spawnChance)
                {
                    // random chance of spawning an animal (bear, pig or stag for now)            
                    int x;
                    int y;
                    int z;
                    try
                    {
                        GameManager.Instance.World.FindRandomSpawnPointNearPosition(
                        _blockPos.ToVector3(), 15, out x, out y, out z,
                        new Vector3(15, 15, 15), true, false);
                        int entityID = EntityGroups.GetRandomFromGroup(entityGroup);
                        Entity spawnEntity = EntityFactory.CreateEntity(entityID,
                            new Vector3((float) x, (float) y, (float) z));
                        spawnEntity.SetSpawnerSource(EnumSpawnerSource.StaticSpawner, _clrIdx, entityGroup);
                        GameManager.Instance.World.SpawnEntityInWorld(spawnEntity);
                        MarkSpawned(_world, _clrIdx, _blockPos, _blockValue);
                        DisplayChatAreaText("Spawned entity");
                        spawnEntity = null;
                    }
                    catch (Exception)
                    {
                        DisplayChatAreaText("Error spawning");
                    }                    
                }
            }
        }
        // if it's an animal trap, it will always autoreset after a while
        bool autoreset = false;
        if (this.Properties.Values.ContainsKey("AutoReset"))
        {
            if (bool.TryParse(this.Properties.Values["AutoReset"], out autoreset) == false) autoreset = false;
        }
        if (autoreset && IsTrapFired(_blockValue.meta))
        {
            _blockValue.meta = (byte) (_blockValue.meta & ~(1 << 0)); // reset triggered flag
            _blockValue.meta = (byte) (_blockValue.meta & ~(1 << 1)); // reset baited flag
            _blockValue.meta = (byte) (_blockValue.meta & ~(1 << 2)); // reset triggered by animal flag
            _blockValue.meta = (byte) (_blockValue.meta & ~(1 << 3)); // reset already spawned animal flag
            _blockValue.meta3 = 0;
            DisplayChatAreaText(string.Format("META = {0}", _blockValue.meta));
            _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
        }
        return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
    }

    private static void MarkSpawned(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
// marks the trap so that it spawns no more animals until rebaited
        _blockValue.meta = (byte) (_blockValue.meta | (1 << 3)); // set spawned animal flag to 1
        _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
    }

    private int DoDamage(Vector3i _blockPos, Entity _targetEntity)
    {
        int result = -1;
        int killChance = 0;
        if (this.Properties.Values.ContainsKey("KillChance"))
        {
            if (int.TryParse(this.Properties.Values["KillChance"], out killChance) == false) killChance = 10;
        }
        int doDamage = 0;
        if (this.Properties.Values.ContainsKey("DamageDone"))
        {
            if (int.TryParse(this.Properties.Values["DamageDone"], out doDamage) == false) doDamage = 10;
        }
        // if it's a trap to capture an alive animal it will always to 99999 of damage.
        string[] animalcap = null;
        char[] splitter = new char[1];
        string[] auxStrings;
        splitter[0] = ',';
        if (this.Properties.Values.ContainsKey("AnimalToCatch"))
        {
            string animAux = this.Properties.Values["AnimalToCatch"];
            auxStrings = animAux.Split(splitter);
            if (auxStrings.Length > 0)
            {
                animalcap = auxStrings;               
            }
            else
            {
                animalcap = new string[1];
                animalcap[0] = animAux;
            }
        }
        if (animalcap != null)
        {
            // if the animal caught is not any of the wanted animals, it does no damage
            // meta3 tells which animal it is...   
            DisplayChatAreaText("Caught " + (_targetEntity as EntityAlive).EntityName);
            if (animalcap[0] == (_targetEntity as EntityAlive).EntityName) result = 1;
            else if (animalcap[1] == (_targetEntity as EntityAlive).EntityName) result = 2;
            if (result > -1)
            {
                doDamage = 9999;
            }
            else doDamage = 0;
        }
        if (doDamage == 0) return -1; // no damage to do
        // check if it should attack legs
        bool hitLegs = false;
        if (this.Properties.Values.ContainsKey("HitLegs"))
        {
            if (bool.TryParse(this.Properties.Values["HitLegs"], out hitLegs) == false) hitLegs = false;
        }
        #region buffs;
        string buffs = ""; // this buffs are applied as soon as you get in the area.
        if (this.Properties.Values.ContainsKey("Buffs"))
        {
            buffs = this.Properties.Values["Buffs"];
        }
        if (buffs != "")
        {
            try
            {
                this.BuffActions = new List<MultiBuffClassAction>();
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
                Debug.Log("SPAWNER - Invalid buffs in trap configuration: " + ex.Message);
            }
        }
        buffs = "";
        if (this.Properties.Values.ContainsKey("CritBuffs"))
        {
            buffs = this.Properties.Values["CritBuffs"];
        }
        if (buffs != "")
        {
            try
            {
                this.critBuffActions = new List<MultiBuffClassAction>();
                string[] buffList = buffs.Split(',');
                if (buffList.Length > 0)
                {
                    foreach (string buffItem in buffList)
                    {
                        MultiBuffClassAction multiBuffClassAction = null;
                        multiBuffClassAction = MultiBuffClassAction.NewAction(buffItem);
                        if (this.critBuffActions == null)
                            this.critBuffActions = new List<MultiBuffClassAction>();
                        this.critBuffActions.Add(multiBuffClassAction);
                    }
                }
                else
                {
                    MultiBuffClassAction multiBuffClassAction = null;
                    multiBuffClassAction = MultiBuffClassAction.NewAction(buffs);
                    if (this.critBuffActions == null)
                        this.critBuffActions = new List<MultiBuffClassAction>();
                    this.critBuffActions.Add(multiBuffClassAction);
                }
            }
            catch (Exception ex)
            {
                this.BuffActions = null;
                Debug.Log("SPAWNER - Invalid buffs in trap configuration: " + ex.Message);
            }
        }
        #endregion;
        // damage
        if (!(_targetEntity is EntityAlive))
            return -1;        
        EntityAlive entityAlive = (EntityAlive)_targetEntity;
        if (entityAlive.IsDead())
            return -1;

        // this traps will randomly kill instantly... like critical hit.. 20% chance
        Random r = new System.Random((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
        int killNow = r.Next(1, 101);
        if (killNow <= killChance)
        {
            doDamage = 9999;
            //if (_targetEntity is EntityPlayer) // boom headshot!
            //    doDamage = 9999;
            //else
            //{
            //    _targetEntity.Kill(DamageResponse.New(true));
            //    return true;
            //}
        }

        EnumDamageSourceType _damageSourceName = EnumDamageSourceType.BlockDamage;
        Vector3 vector3 = 6f *
                          (_targetEntity.transform.position -
                           new Vector3((float)_blockPos.x + 0.5f, (float)_blockPos.y, (float)_blockPos.z + 0.5f));

        DamageSourceEntity damageSourceEntity = new DamageSourceEntity(EnumDamageSourceType.BlockDamage, -1);
        damageSourceEntity.SetIgnoreConsecutiveDamages(true);
        bool flag = false;
        if (entityAlive is EntityZombie && !(entityAlive is EntityZombieDog) && !(entityAlive is EntityAnimalClown) && !(entityAlive is EntityAnimalHal))
        {
            if (entityAlive.IsImmuneToLegDamage || !hitLegs || doDamage >= 9999)
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
            // check if critical buffs should be applyed instead
            List<MultiBuffClassAction> auxBuffs = this.BuffActions;
            if (critBuffActions != null)
            {
                if (critBuffActions.Count > 0)
                    if (r.Next(1, 101) <= 10) auxBuffs = this.critBuffActions;
            }
            // apply buffs
            if (auxBuffs != null)
            {
                if (auxBuffs.Count > 0)
                {
                    using (List<MultiBuffClassAction>.Enumerator enumerator = auxBuffs.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                            enumerator.Current.Execute(this.blockID, (EntityAlive)_targetEntity, false,
                                EnumBodyPartHit.None, (string)null);
                    }
                }
            }
        }
        return result;
    }
}