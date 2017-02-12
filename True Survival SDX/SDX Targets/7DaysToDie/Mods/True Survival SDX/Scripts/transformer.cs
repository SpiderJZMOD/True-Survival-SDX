using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// this class is basically a "transformer"
/// they do not produce energy per se, but they transform items
/// they need a catalist (or fuel), a raw material, and a emtpy vessel for the end product
/// catalist, raw material, vessel and end product items are configurable
/// if any are left blank they will be ignored
/// Mortelentus - 2016
/// </summary>
public class BlockTransformer : BlockLoot
{
    private bool debug = false;

    private bool disableDebug = true;
    EmitterScript script;
    UnityEngine.GameObject gameObject;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    public BlockTransformer()
    {
        //this.IsRandomlyTick = true;
    }

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
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

    public override void Init()
    {
        base.Init();
        //this.IsRandomlyTick = true;
    }

    public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx, Vector3i _blockPos,
        BlockValue _blockValue, EntityAlive _player)
    {
        //DisplayChatAreaText(string.Format("TICK random: {0}, TICK rate: {1}", this.IsRandomlyTick.ToString(), this.GetTickRate()));
        return base.OnBlockActivated(_indexInBlockActivationCommands, _world, _cIdx, _blockPos, _blockValue, _player);
    }

    public override BlockValue OnBlockPlaced(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, Random _rnd)
    {
        //_blockValue.meta = (byte)_rnd.Next(16); // tick ?
        return base.OnBlockPlaced(_world, _clrIdx, _blockPos, _blockValue, _rnd);
    }

    public override ulong GetTickRate()
    {
        ulong result = 10;
        if (this.Properties.Values.ContainsKey("TickRate"))
        {
            if (ulong.TryParse(this.Properties.Values["TickRate"], out result) == false) result = 10; 
        }
        return result;
    }

    public override void OnBlockAdded(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        //DisplayChatAreaText(string.Format("TICK random: {0}, TICK rate: {1}, BLOCKID: {2}", this.IsRandomlyTick.ToString(), this.GetTickRate(), this.blockID));
        base.OnBlockAdded(world, _chunk, _blockPos, _blockValue);
        //if (!world.IsRemote())
        //{
        //    world.GetWBT().AddScheduledBlockUpdate(_chunk.ClrIdx, _blockPos, this.blockID, this.GetTickRate());
        //}        
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        //try
        //{
        //    DoTransform(_world, _clrIdx, _blockPos, _blockValue, true);
        //}
        //catch (Exception)
        //{
            
        //}        
        //finally 
        //{
        //    // add next tick - if no transformation is possible, no use to stress the system
        //    // it will be done next time something is added to the container
        //    _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
        //}
        return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
        //return true;
    }

    // process transformation on each tick
    public bool DoTransform(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, bool produce)
    {       
        //Debug.Log("TRANSFORMER TICK");
        bool resultado = false;
        try
        {
            char[] splitter = new char[1];
            string[] auxStrings;
            splitter[0] = ',';
            string fuelName = "";
            string emptyName = "";
            string rawName = "";
            string productName = "";
            ItemValue FuelObject = (ItemValue) null;
            ItemValue RawObject = (ItemValue) null;
            ItemValue EmptyObject = (ItemValue) null;
            ItemValue ProductObject = (ItemValue) null;
            int fuelNumber = 0;
            int rawNumber = 0;
            int decayValue = 0;
            int emptyNumber = 1;
            int productNumber = 1;
            try
            {
                #region finds the fuel object

                if (this.Properties.Values.ContainsKey("FuelName"))
                {
                    fuelName = this.Properties.Values["FuelName"];
                    auxStrings = fuelName.Split(splitter);
                    if (auxStrings.Length > 1)
                    {
                        fuelName = auxStrings[0].Trim();
                        if (int.TryParse(auxStrings[1].Trim(), out fuelNumber) == false) fuelNumber = 0;
                    }
                }
                FuelObject = ItemClass.GetItem(fuelName);
                DisplayChatAreaText(string.Format("FUEL: {0}x{1} - {2}", fuelNumber, fuelName, FuelObject.ToString()));

                #endregion;

                #region Find the catalyst object and number

                if (this.Properties.Values.ContainsKey("RawName"))
                {
                    rawName = this.Properties.Values["RawName"];
                    auxStrings = rawName.Split(splitter);
                    if (auxStrings.Length > 1)
                    {
                        rawName = auxStrings[0].Trim();
                        if (int.TryParse(auxStrings[1].Trim(), out rawNumber) == false) rawNumber = 0;
                    }
                }
                if (rawName != "")
                {
                    RawObject = ItemClass.GetItem(rawName);
                    // gets the catalyst number
                    DisplayChatAreaText(string.Format("RAW MAT: {0}x{1} - {2}", rawNumber, rawName, RawObject.ToString()));
                }

                #endregion;

                #region finds the emtpy object and empty number per production

                if (this.Properties.Values.ContainsKey("EmptyName"))
                {
                    emptyName = this.Properties.Values["EmptyName"];
                    auxStrings = emptyName.Split(splitter);
                    if (auxStrings.Length >= 1)
                    {
                        emptyName = auxStrings[0].Trim();
                        if (auxStrings.Length > 1)
                            if (int.TryParse(auxStrings[1].Trim(), out emptyNumber) == false) emptyNumber = 1;
                    }                   
                }
                if (emptyName != "")
                {
                    EmptyObject = ItemClass.GetItem(emptyName);
                    DisplayChatAreaText(string.Format("VESSEL: {2}x{0} - {1}", emptyName, EmptyObject.ToString(), emptyNumber));
                }
                #endregion;            

                #region finds the product object and Product number to create 

                if (this.Properties.Values.ContainsKey("ProductName"))
                {
                    productName = this.Properties.Values["ProductName"];
                    auxStrings = productName.Split(splitter);
                    if (auxStrings.Length >= 1)
                    {
                        productName = auxStrings[0].Trim();
                        if (auxStrings.Length > 1)
                            if (int.TryParse(auxStrings[1].Trim(), out productNumber) == false) productNumber = 1;
                    }
                }
                if (productName != "")
                {
                    ProductObject = ItemClass.GetItem(productName);
                   
                    DisplayChatAreaText(string.Format("END PRODUCT: {2}x{0} - {1}", productName, ProductObject.ToString(), productNumber));                   
                }

                #endregion;            

                #region Decay value;
                if (this.Properties.Values.ContainsKey("DecayRate"))
                {
                    if (int.TryParse(this.Properties.Values["DecayRate"], out decayValue) == false) decayValue = 0;
                }
                #endregion;
            }
            catch (Exception ex)
            {
               Debug.Log("WARNING - no fuel found");
            }
            if (FuelObject != null)
            {
                // try to find the godamn loot container list
                //TileEntitySecureLootContainer Container;
                TileEntityLootContainer Container;
                TileEntity tileEntity = (TileEntity) null;

                tileEntity = _world.GetTileEntity(_cIdx, _blockPos);
                if (tileEntity != null)
                {
                    if (tileEntity is TileEntityLootContainer)
                    {
                        Container = (TileEntityLootContainer)tileEntity;
                        // finds first with fuel and stack>=fuelnumber
                        // finds first with raw and stack>=rawnumber if defined
                        // finds first emtpy
                        // finds first with emptyshell if defined.
                        // finds first with end product.
                        ItemStack fuelStack = (ItemStack) null;
                        ItemStack rawStack = (ItemStack) null;
                        ItemStack vesselStack = (ItemStack) null;
                        ItemStack productStack = (ItemStack) null;
                        ItemStack emtpyStack = (ItemStack) null;
                        foreach (ItemStack itemStack1 in Container.items)
                        {
                            if (FuelObject != null && fuelStack == null)
                            {
                                #region Checks fuel stack if still needed

                                if (itemStack1.itemValue.Equals(FuelObject))
                                {
                                    if (itemStack1.count >= fuelNumber)
                                    {
                                        DisplayChatAreaText("FUEL STACK FOUND");
                                        fuelStack = itemStack1;
                                    }
                                }

                                #endregion;
                            }
                            if (RawObject != null && rawStack == null)
                            {
                                #region Checks raw stack if still needed

                                if (itemStack1.itemValue.Equals(RawObject))
                                {
                                    if (itemStack1.count > rawNumber)
                                    {
                                        DisplayChatAreaText("RAW STACK FOUND");
                                        rawStack = itemStack1;
                                    }
                                }

                                #endregion;
                            }
                            // empty stack
                            if (itemStack1.count == 0 && emtpyStack == null)
                            {
                                DisplayChatAreaText("EMPTY SLOT FOUND");
                                emtpyStack = itemStack1;
                            }
                            if (EmptyObject != null && vesselStack == null)
                            {
                                #region Checks emtpy object if needed

                                if (itemStack1.count >= emptyNumber && itemStack1.itemValue.Equals(EmptyObject))
                                {
                                    DisplayChatAreaText("VESSEL STACK FOUND");
                                    vesselStack = itemStack1;
                                }

                                #endregion;
                            }
                            if (ProductObject != null && productStack == null)
                            {
                                #region Checks product object if needed

                                if (itemStack1.count > 0 && itemStack1.itemValue.Equals(ProductObject) && itemStack1.CanStack(productNumber))
                                {
                                    DisplayChatAreaText("PRODUCT STACK FOUND");
                                    productStack = itemStack1;
                                }

                                #endregion;
                            }
                            if (fuelStack != null && (EmptyObject != null && vesselStack != null) && emtpyStack != null &&
                                (RawObject != null && rawStack != null) &&
                                (ProductObject != null && productStack != null))
                            {
                                DisplayChatAreaText("CAN STOP SEARCHING");
                                break;
                            }
                        }
                        if (fuelStack != null)
                        {
                            DisplayChatAreaText(fuelName + " exists count = " + fuelStack.count.ToString());
                            // checks if it can actually produce anything
                            bool canProduce = true;
                            if (FuelObject != null && fuelStack == null)
                            {
                                canProduce = false;
                                DisplayChatAreaText("PRODUCTION NOT POSSIBLE - FUEL MISSING");
                            }
                            else if (RawObject != null && rawStack == null)
                            {
                                canProduce = false;
                                DisplayChatAreaText("PRODUCTION NOT POSSIBLE - RAW MATERIAL MISSING");
                            }
                            else if (emtpyStack == null && productStack == null)
                            {
                                canProduce = false;
                                DisplayChatAreaText("PRODUCTION NOT POSSIBLE - NO EMPTY STLOT");
                            }
                            else if (EmptyObject != null && vesselStack == null)
                            {
                                canProduce = false;
                                DisplayChatAreaText("PRODUCTION NOT POSSIBLE - VESSEL MISSING");
                            }
                            else if (ProductObject == null)
                            {
                                canProduce = false;
                                DisplayChatAreaText("PRODUCTION NOT POSSIBLE - NO FINAL PRODUCT DEFINED");
                            }
                            if (canProduce)
                            {
                                DisplayChatAreaText(string.Format("WILL TRY TO PRODUCE - meta2 = {0}, IsOn = {1}", _blockValue.meta2, BlockTransformer.IsOn(_blockValue.meta2)));
                                if (!BlockTransformer.IsOn(_blockValue.meta2))
                                {
                                    // if not with animation On, set it now, I will be using the bit 0.
                                    _blockValue.meta2 = (byte) (_blockValue.meta2 | (1 << 0));
                                    _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
                                }                               
                                // this function can be called, just to verify
                                if (produce)
                                {
                                    if (decayValue > 0)
                                    {
                                        // do decay
                                        Block.list[_blockValue.type].DamageBlock(_world, _cIdx, _blockPos, _blockValue, decayValue,
                                            tileEntity.entityId, false);
                                    }
                                    if (fuelStack != null)
                                        fuelStack.count = fuelStack.count - fuelNumber; // removes fuel
                                    if (rawStack != null)
                                        rawStack.count = rawStack.count - rawNumber; // removes raw mat
                                    if (vesselStack != null)
                                        vesselStack.count = vesselStack.count - emptyNumber;
                                    // removes 1 emtpy shell                            
                                    if (ProductObject != null)
                                    {
                                        if (productStack != null)
                                        {
                                            DisplayChatAreaText("Adding " + productName);
                                            productStack.count = productStack.count + productNumber;
                                        }
                                        else if (emtpyStack != null)
                                        {
                                            DisplayChatAreaText("Creating " + productName);
                                            emtpyStack.itemValue = ProductObject;
                                            emtpyStack.count = productNumber;
                                        }
                                    }
                                    Container.SetModified(); // apply moddification                                
                                }
                            }
                            else
                            {
                                DisplayChatAreaText(string.Format("NOT ENOUGH MATS - meta2 = {0}, IsOn = {1}", _blockValue.meta2, BlockTransformer.IsOn(_blockValue.meta2)));
                                if (BlockTransformer.IsOn(_blockValue.meta2))
                                {
                                    // if not with animation Off, set it now, I will be using the bit 0.
                                    _blockValue.meta2 = (byte) (_blockValue.meta2 & ~(1 << 0));
                                    _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
                                }
                            }
                            resultado = true;
                        }
                        else
                        {
                            DisplayChatAreaText(string.Format("NOT FUEL AVAILABLE - meta2 = {0}, IsOn = {1}", _blockValue.meta2, BlockTransformer.IsOn(_blockValue.meta2)));
                            if (BlockTransformer.IsOn(_blockValue.meta2))
                            {
                                // if not with animation Off, set it now, I will be using the bit 0.
                                _blockValue.meta2 = (byte)(_blockValue.meta2 & ~(1 << 0));
                                _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
                            }
                        }
                    }
                }
                else
                    DisplayChatAreaText("No container for this object");
            }
            else DisplayChatAreaText("No fuel configured");
        }
        catch (Exception ex1)
        {
            DisplayChatAreaText(ex1.Message);
        }
        return resultado;
    }

    public static bool IsOn(byte _metadata)
    {
        return ((int)_metadata & 1 << 0) != 0;
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        // the animations need to be triggered here so that they are shown to all players
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        if ((int)_oldBlockValue.meta2 == (int)_newBlockValue.meta2) return;
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
            if (BlockTransformer.IsOn(_blockValue.meta2) && !BlockTransformer.IsOn(_oldBlockValue.meta2))
            {
                // play open animation
                DisplayChatAreaText("Turn On");
                animator.ResetTrigger("offTrigger");
                animator.SetTrigger("onTrigger");                
            }
            else if (!BlockTransformer.IsOn(_blockValue.meta2) && BlockTransformer.IsOn(_oldBlockValue.meta2))
            {
                // play close animation
                DisplayChatAreaText("Turn Off");
                animator.ResetTrigger("onTrigger");
                animator.SetTrigger("offTrigger");
            }
        }
        if (!_world.IsRemote()) // only runs on server        
        {
            if (AddScript(_blockValue.meta2) && !AddScript(_oldBlockValue.meta2))
            {
                // resets the bit
                _blockValue.meta2 = (byte)(_blockValue.meta2 & ~(1 << 1));
                _world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                // get the transform
                if (_ebcd != null)
                {
                    try
                    {
                        gameObject = _ebcd.transform.gameObject;
                        // adds the script if still not existing.
                        script = gameObject.GetComponent<EmitterScript>();
                        if (script == null)
                        {
                            if (!disableDebug) Debug.Log("TRANSFORMER: ADDING SCRIPT");
                            script = gameObject.AddComponent<EmitterScript>();
                            script.initialize(_world, _blockPos, _clrIdx);
                        }
                        else if (!disableDebug) Debug.Log("TRANSFORMER: OnBlockValueChanged - SCRIPT ALREADY EXISTING AND RUNNING?");
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("TRANSFORMER: Error OnBlockValueChanged - " + ex.Message);
                    }
                }
            }
        }
    }

    public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
    {
        Animator[] componentsInChildren;
        if (_ebcd == null || !_ebcd.bHasTransform ||
            (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Animator>(false)) == null)
            return;
        bool _isOn = BlockTransformer.IsOn(_blockValue.meta2);
        DisplayChatAreaText(string.Format("MACHINE ON ={0}", _isOn.ToString()));
        foreach (Animator animator in componentsInChildren)
        {
            if (!_isOn)
                animator.CrossFade("off", 0.0f);
            else animator.CrossFade("on", 0.0f);
        }
    }

    public override void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {        
        //DisplayChatAreaText("ON BLOCK LOADED");
        base.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);
        // every time the block is "reloaded" i try to readd it to the ticks, just in case it has stopped running
        //if (!_world.IsRemote())
        //    _world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, this.blockID, this.GetTickRate());
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx,
        BlockValue _blockValue, BlockEntityData _ebcd)
    {
        DisplayChatAreaText("TRANSFORMER: OnBlockEntityTransformAfterActivated");
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        // every time this happens?
        //int radiationArea = 0;
        //if (this.Properties.Values.ContainsKey("RadiationArea"))
        //{
        //    if (int.TryParse(this.Properties.Values["RadiationArea"], out radiationArea) == false)
        //        radiationArea = 0;
        //}
        //if (radiationArea > 0)
        {
            // set bit to 1
            if (!AddScript(_blockValue.meta2))
            {
                _blockValue.meta2 = (byte) (_blockValue.meta2 | (1 << 1));
                _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
            }
        }
    }

    private bool AddScript(byte _metadata)
    {
        return ((int)_metadata & 1 << 1) != 0;
    }
}

public class EmitterScript : MonoBehaviour
{
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
    ulong emissionRate = 10; //in seconds
    ulong transformRate = 10; //in seconds
    int pauseTime = 0; // in minutes
    private int numberToPause = 0;
    DateTime dtaNextEmit = DateTime.MinValue;
    DateTime dtaNextSpawn = DateTime.MinValue;
    DateTime dtaNextTransform = DateTime.MinValue;
    DateTime dtaNextCheck = DateTime.MinValue;
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
        emissionRate = 10;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("EmissionRate"))
        {
            if (ulong.TryParse(Block.list[blockValue.type].Properties.Values["EmissionRate"], out emissionRate) == false) emissionRate = 10;
        }
        // the area inside wich there is radiation area and buffs apply
        radiationArea = 0;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("RadiationArea"))
        {
            if (int.TryParse(Block.list[blockValue.type].Properties.Values["RadiationArea"], out radiationArea) == false)
                radiationArea = 0;
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
                Debug.Log("EMITTER - Invalid buffs in spawner configuration: " + ex.Message);
            }
        }

        #endregion;
        transformRate = 10;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("TickRate"))
        {
            if (ulong.TryParse(Block.list[blockValue.type].Properties.Values["TickRate"], out transformRate) == false) transformRate = 10;
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
            if (DateTime.Now > dtaNextTransform)
            {
                dtaNextTransform = DateTime.Now.AddSeconds(transformRate);
                try
                {
                    blockValue = world.GetBlock(cIdx, blockPos);
                    (Block.list[blockValue.type] as BlockTransformer).DoTransform(world, cIdx, blockPos, blockValue,
                        true);
                }
                catch (Exception ex)
                {
                    Debug.Log("TRANFORMER: ERROR - " + ex.Message);                    
                }                
            }
            else if (DateTime.Now > dtaNextCheck)
            {
                // only checks if it should be on / off
                dtaNextCheck = DateTime.Now.AddSeconds(1);
                try
                {
                    blockValue = world.GetBlock(cIdx, blockPos);
                    (Block.list[blockValue.type] as BlockTransformer).DoTransform(world, cIdx, blockPos, blockValue,
                        false);
                }
                catch (Exception ex)
                {
                    Debug.Log("TRANFORMER: ERROR - " + ex.Message);
                }
            }
            if (DateTime.Now > dtaNextEmit)
            {
                #region Damage/Buff emission
                try
                {
                    blockValue = world.GetBlock(cIdx, blockPos);
                    dtaNextEmit = DateTime.Now.AddSeconds(emissionRate);                    
                    if (BlockTransformer.IsOn(blockValue.meta2))
                    {
                        // check if I can mark it as on already
                        // checks if there is any player near
                        // spawned entities
                        //SortedDictionary<int, Entity> spawnedEntities = new SortedDictionary<int, Entity>();       
                        if (radiationArea > 0 && (radiationDamage > 0 || buffs != null))
                        {
                            foreach (Entity entity in GameManager.Instance.World.Entities.list)
                            {
                                if (entity is EntityPlayer && entity.IsAlive())
                                {
                                    try
                                    {
                                        float distance =
                                            Math.Abs(Vector3.Distance(entity.position, blockPos.ToVector3()));
                                        if (distance <= radiationArea && radiationArea > 0)
                                            doRadiationDamage(entity as EntityPlayer);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.Log("EMITTER: ERROR (1) - " + ex.Message);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    dtaNextEmit = DateTime.Now.AddSeconds(emissionRate);
                    Debug.Log("EMITTER: ERROR (2) - " + ex.Message);
                }
                #endregion;
                try
                {
                    alzheimer();
                }
                catch (Exception ex)
                {
                    dtaNextEmit = DateTime.Now.AddSeconds(emissionRate);
                    Debug.Log("EMITTER: ERROR (3) - " + ex.Message);
                }                
            }
        }
    }

    private void doRadiationDamage(EntityPlayer player)
    {
        if (radiationDamage > 0)
        {
            player.DamageEntity(DamageSource.radiation, radiationDamage, false, 1f);
        }
        if (this.BuffActions != null)
        {
            if (BuffActions.Count > 0)
            {
                using (List<MultiBuffClassAction>.Enumerator enumerator = BuffActions.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                        enumerator.Current.Execute(blockValue.type, (EntityAlive)player, false,
                            EnumBodyPartHit.None, (string)null);
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
}