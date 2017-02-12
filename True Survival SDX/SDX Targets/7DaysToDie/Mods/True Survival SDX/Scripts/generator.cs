using System;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// this class is basically a energy generator
/// it will produce energy while fueled (or on we'll see)
/// they can be used in the power mode
/// Mortelentus - 2016
/// </summary>
public class BlockGenerator : BlockLoot
{
    private BlockActivationCommand[] OO = new BlockActivationCommand[3]
    {
        new BlockActivationCommand("open", "door", false),
        new BlockActivationCommand("turn on", "lock", false),
        new BlockActivationCommand("turn off", "unlock", false),
    };

    private bool debug = false;
    GeneratorScript script;
    UnityEngine.GameObject gameObject;

    private bool disableDebug = true;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    public BlockGenerator()
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

    public override BlockValue OnBlockPlaced(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue,
        Random _rnd)
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
        //DisplayChatAreaText(string.Format("TICK random: {0}, TICK rate: {1}, BLOCKID: {2}",
        //    this.IsRandomlyTick.ToString(), this.GetTickRate(), this.blockID));
        base.OnBlockAdded(world, _chunk, _blockPos, _blockValue);
        //if (!world.IsRemote())
        //{
        //    world.GetWBT().AddScheduledBlockUpdate(_chunk.ClrIdx, _blockPos, this.blockID, this.GetTickRate());
        //}
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue,
        bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        //try
        //{
        //    CheckPower(_world, _clrIdx, _blockPos, _blockValue);
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

    public static bool IsOn(byte _metadata)
    {
        bool turnedOn = SwitchOn( _metadata);
        bool animOn = AnimOn(_metadata);
        return animOn && turnedOn;
    }

    public static bool AnimOn(byte _metadata)
    {
        return ((int)_metadata & 1 << 0) != 0;
    }

    public static bool SwitchOn(byte _metadata)
    {
        return ((int) _metadata & 1 << 1) != 0;
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos,
        BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        // the animations need to be triggered here so that they are shown to all players
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        if ((int) _oldBlockValue.meta2 == (int) _newBlockValue.meta2) return;
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
            if (BlockGenerator.IsOn(_blockValue.meta2) && !BlockGenerator.IsOn(_oldBlockValue.meta2))
            {
                // play open animation
                DisplayChatAreaText("Turn On");
                animator.ResetTrigger("offTrigger");
                animator.SetTrigger("onTrigger");
            }
            else if (!BlockGenerator.IsOn(_blockValue.meta2) && BlockGenerator.IsOn(_oldBlockValue.meta2))
            {
                // play close animation
                DisplayChatAreaText("Turn Off");
                animator.ResetTrigger("onTrigger");
                animator.SetTrigger("offTrigger");
            }
        }
        if (!_world.IsRemote()) // only runs on server        
        {
            //if (AddScript(_blockValue.meta2) && !AddScript(_oldBlockValue.meta2))
            {
                // resets the bit
                //_blockValue.meta2 = (byte)(_blockValue.meta2 & ~(1 << 2));
                //_world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
                // get the transform
                if (_ebcd != null)
                {
                    try
                    {
                        gameObject = _ebcd.transform.gameObject;
                        // adds the script if still not existing.
                        script = gameObject.GetComponent<GeneratorScript>();
                        if (script == null)
                        {
                            if (!disableDebug) Debug.Log("GENERATOR: ADDING SCRIPT");
                            script = gameObject.AddComponent<GeneratorScript>();
                            script.initialize(_world, _blockPos, _clrIdx);
                        }
                        else if (!disableDebug) Debug.Log("GENERATOR: OnBlockValueChanged - SCRIPT ALREADY EXISTING AND RUNNING?");
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("GENERATOR: Error OnBlockValueChanged - " + ex.Message);
                    }
                }
            }
            if (!disableDebug) Debug.Log("GENERATOR: OnBlockValueChanged - NO NEED TO ADD SCRIPT");
        }
        else if (!disableDebug) Debug.Log("GENERATOR: OnBlockValueChanged - REMOTE");
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
        Animator[] componentsInChildren;
        if (_ebcd == null || !_ebcd.bHasTransform ||
            (componentsInChildren = _ebcd.transform.GetComponentsInChildren<Animator>(false)) == null)
            return;
        bool _isOn = BlockGenerator.IsOn(_blockValue.meta2);
        //DisplayChatAreaText(string.Format("MACHINE ON ={0}", _isOn.ToString()));
        foreach (Animator animator in componentsInChildren)
        {
            if (!_isOn)
                animator.CrossFade("off", 0.0f);
            else animator.CrossFade("on", 0.0f);
        }        
    }

    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        this.OO[0].enabled = true;
        this.OO[1].enabled = !BlockGenerator.SwitchOn(_blockValue.meta2);
        this.OO[2].enabled = BlockGenerator.SwitchOn(_blockValue.meta2);
        return this.OO;
    }

    public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx,
        Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        switch (_indexInBlockActivationCommands)
        {
            case 0:
            case 3:
                return base.OnBlockActivated(_indexInBlockActivationCommands, _world, _cIdx, _blockPos, _blockValue, _player);
                return false;
            case 1:
                // turn it on - bit 1
                if (!SwitchOn(_blockValue.meta2))
                {
                    _blockValue.meta2 = (byte) (_blockValue.meta2 | (1 << 1));
                    _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
                }
                return false;
            case 2:
                // turn it off - bit 1
                if (SwitchOn(_blockValue.meta2))
                {
                    _blockValue.meta2 = (byte) (_blockValue.meta2 & ~(1 << 1));
                    _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
                }
                return true;                        
            default:
                return false;
        }       
    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos,
        EntityAlive _entityFocusing)
    {
        string text = base.GetActivationText(_world, _blockValue, _clrIdx, _blockPos,
        _entityFocusing);
        if (BlockGenerator.SwitchOn(_blockValue.meta2)) text = string.Format("{0} ({1})", text, "Switched ON");
        else text = string.Format("{0} ({1})", text, "Switched OFF");
        return text;
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx,
        BlockValue _blockValue, BlockEntityData _ebcd)
    {
        DisplayChatAreaText("GENERATOR: OnBlockEntityTransformAfterActivated");
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        // do this every time this happens, might be easier
        // just add the damn script, no matter what...
        //if (!AddScript(_blockValue.meta2))
        {
            //_blockValue.meta2 = (byte) (_blockValue.meta2 | (1 << 2));
            _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
        }
    }

    private bool AddScript(byte _metadata)
    {
        // bit 2
        return ((int)_metadata & 1 << 2) != 0;
    }
}

// monobehaviour script, running in the background
// on/off is always checked
// fuel is consumed at xml ratio
public class GeneratorScript : MonoBehaviour
{
    private bool debug = false;
    private WorldBase world;
    private Vector3i blockPos;
    private int cIdx;
    DateTime dtaNextTick = DateTime.MinValue;
    DateTime dtaNextFuel = DateTime.MinValue;
    DateTime dtaNextDecay = DateTime.MinValue;
    ulong tickRate = 1; //in seconds - minimum value
    ulong fuelRate = 5; //in seconds - minimum value
    ulong decayTime = 30; //in seconds - 0 is disabled
    // cycle variables
    int decayValue = 0;
    char[] splitter = new char[1];
    string[] auxStrings;    
    string fuelName = "";
    ItemValue FuelObject = (ItemValue)null;
    int fuelNumber = 0;
    string powerSource = "";
    int powerRadius = 0;

    void Start()
    {

    }

    public void initialize(WorldBase _world, Vector3i _blockPos,
        int _cIdx)
    {
        blockPos = _blockPos;
        cIdx = _cIdx;
        splitter[0] = ',';
        // fill properties
        BlockValue blockValue = _world.GetBlock(cIdx, blockPos);
        tickRate = 1;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("TickRate"))
        {
            if (ulong.TryParse(Block.list[blockValue.type].Properties.Values["TickRate"], out tickRate) == false) tickRate = 1;
        }
        fuelRate = 5;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("FuelRate"))
        {
            if (ulong.TryParse(Block.list[blockValue.type].Properties.Values["FuelRate"], out fuelRate) == false) fuelRate = 5;
        }
        decayTime = 0;
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("DecayTime"))
        {
            if (ulong.TryParse(Block.list[blockValue.type].Properties.Values["DecayTime"], out decayTime) == false) decayTime = 0;
        }
        if (Block.list[blockValue.type].Properties.Values.ContainsKey("debug"))
        {
            if (bool.TryParse(Block.list[blockValue.type].Properties.Values["debug"], out debug) == false) debug = false;
        }
        try
        {
            // for example gas cans

            #region finds the fuel object

            if (Block.list[blockValue.type].Properties.Values.ContainsKey("FuelName"))
            {
                fuelName = Block.list[blockValue.type].Properties.Values["FuelName"];
                auxStrings = fuelName.Split(splitter);
                if (auxStrings.Length > 1)
                {
                    fuelName = auxStrings[0].Trim();
                    if (int.TryParse(auxStrings[1].Trim(), out fuelNumber) == false) fuelNumber = 0;
                }
            }
            // if fuel number is 0, it means that it will look for the specified transformer nearby
            // and check if any of them is on.
            if (fuelNumber > 0)
            {
                FuelObject = ItemClass.GetItem(fuelName);
                if (debug) Debug.Log(string.Format("FUEL: {0}x{1} - {2}", fuelNumber, fuelName, FuelObject.ToString()));
            }
            else
            {
                // locks for power emitter
                if (Block.list[blockValue.type].Properties.Values.ContainsKey("PowerEmitter"))
                {
                    powerSource = Block.list[blockValue.type].Properties.Values["PowerEmitter"];
                    auxStrings = powerSource.Split(splitter);
                    if (auxStrings.Length > 1)
                    {
                        powerSource = auxStrings[0].Trim();
                        if (int.TryParse(auxStrings[1].Trim(), out powerRadius) == false) powerRadius = 5;
                    }
                }
            }

            #endregion;

            // generator WILL decay if not repaired

            #region Decay value;

            if (Block.list[blockValue.type].Properties.Values.ContainsKey("DecayRate"))
            {
                if (int.TryParse(Block.list[blockValue.type].Properties.Values["DecayRate"], out decayValue) == false) decayValue = 0;
            }
            if (decayTime == 0) decayValue = 0;

            #endregion;
        }
        catch (Exception ex)
        {
            if (debug) Debug.Log("WARNING - no fuel found");
        }
        world = _world;
    }

    void Update()
    {
        if (world != null)
        {
            //if (debug) Debug.Log("GENERATOR FRAME");
            // does this every second, no need to be every frame or it will stress too damn much
            if (DateTime.Now > dtaNextTick)
            {
                dtaNextTick = DateTime.Now.AddSeconds(tickRate);
                try
                {
                    CheckPower(world, cIdx, blockPos);
                }
                catch (Exception ex)
                {
                    if (debug) Debug.Log("ERROR GENERATOR TICK - " + ex.Message);
                }
            }
        }
    }

    public bool CheckPower(WorldBase _world, int _cIdx, Vector3i _blockPos)
    {
        if (debug) Debug.Log("GENERATOR TICK");
        BlockValue _blockValue = _world.GetBlock(_cIdx, _blockPos);
        bool resultado = false;
        if (!BlockGenerator.SwitchOn(_blockValue.meta2))
        {
            // play off animation if needed and nothing else
            if (BlockGenerator.AnimOn(_blockValue.meta2))
            {
                // if not with animation Off, set it now, I will be using the bit 0.
                _blockValue.meta2 = (byte)(_blockValue.meta2 & ~(1 << 0));
                _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
            }
            return resultado;
        }                
        TileEntity tileEntity = (TileEntity)null;
        tileEntity = _world.GetTileEntity(_cIdx, _blockPos);
        if (tileEntity == null)
        {
            if (debug) Debug.Log("No container for this object");
            return false;
        }
        try
        {                        
            if (powerSource != "" && powerRadius > 0)
            {
                if (debug) Debug.Log("LOOKING FOR POWER SOURCE " + powerSource);
                #region Looks for power source;
                for (int i = _blockPos.x - powerRadius; i <= (_blockPos.x + powerRadius); i++)
                {
                    for (int j = _blockPos.z - powerRadius; j <= (_blockPos.z + powerRadius); j++)
                    {
                        for (int k = _blockPos.y - powerRadius; k <= (_blockPos.y + powerRadius); k++)
                        {
                            BlockValue block = _world.GetBlock(_cIdx, new Vector3i(i, k, j));
                            if (Block.list[block.type].GetBlockName() == powerSource)
                            {
                                //blocks = blocks + block.type + ", ";
                                if (BlockTransformer.IsOn(block.meta2))
                                {
                                    // found a valid power source
                                    if (debug) Debug.Log("FOUND ACTIVE POWER SOURCE");
                                    resultado = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (resultado)
                {
                    if (!BlockGenerator.AnimOn(_blockValue.meta2))
                    {
                        // if not with animation On, set it now, I will be using the bit 0.
                        _blockValue.meta2 = (byte)(_blockValue.meta2 | (1 << 0));
                        _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
                    }
                }
                else
                {
                    if (BlockGenerator.AnimOn(_blockValue.meta2))
                    {
                        // if not with animation Off, set it now, I will be using the bit 0.
                        _blockValue.meta2 = (byte)(_blockValue.meta2 & ~(1 << 0));
                        _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
                    }
                }
                #endregion;
            }
            else if (FuelObject != null)
            {
                #region fuel consumption;
                // try to find the godamn loot container list
                //TileEntitySecureLootContainer Container;
                TileEntityLootContainer Container;
                if (tileEntity != null)
                {
                    if (tileEntity is TileEntityLootContainer)
                    {
                        Container = (TileEntityLootContainer)tileEntity;
                        ItemStack fuelStack = (ItemStack)null;
                        foreach (ItemStack itemStack1 in Container.items)
                        {
                            if (FuelObject != null && fuelStack == null)
                            {
                                #region Checks fuel stack if still needed

                                if (itemStack1.itemValue.Equals(FuelObject))
                                {
                                    if (itemStack1.count >= fuelNumber)
                                    {
                                        if (debug) Debug.Log("FUEL STACK FOUND");
                                        fuelStack = itemStack1;
                                        break;
                                    }
                                }

                                #endregion;
                            }
                        }
                        if (fuelStack != null)
                        {
                            if (debug) Debug.Log(fuelName + " exists count = " + fuelStack.count.ToString());
                            // checks if it can actually produce anything
                            bool hasPower = true;
                            if (FuelObject != null && fuelStack == null)
                            {
                                hasPower = false;
                                if (debug) Debug.Log("NO POWER - FUEL IS MISSING");
                            }
                            if (hasPower)
                            {
                                if (debug) Debug.Log(string.Format("WILL CONSUME FUEL - meta2 = {0}, IsOn = {1}",
                                    _blockValue.meta2, BlockGenerator.AnimOn(_blockValue.meta2)));
                                if (!BlockGenerator.AnimOn(_blockValue.meta2))
                                {
                                    // if not with animation On, set it now, I will be using the bit 0.
                                    _blockValue.meta2 = (byte)(_blockValue.meta2 | (1 << 0));
                                    _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
                                }
                                //here we add the fuel check consume interval
                                if (DateTime.Now > dtaNextFuel)
                                {
                                    dtaNextFuel = DateTime.Now.AddSeconds(fuelRate);
                                    if (fuelStack != null)
                                        fuelStack.count = fuelStack.count - fuelNumber;
                                    // removes fuel                                
                                    Container.SetModified(); // apply moddification   
                                    // heatmat
                                    if (GameManager.Instance.World.aiDirector != null)
                                    {
                                        if (Block.list[_blockValue.type].HeatMapStrength > 0 && Block.list[_blockValue.type].HeatMapWorldTime > 0)
                                        {
                                            GameManager.Instance.World.aiDirector.NotifyActivity(
                                                EnumAIDirectorChunkEvent.Sound, _blockPos, Block.list[_blockValue.type].HeatMapStrength,
                                                Block.list[_blockValue.type].HeatMapWorldTime);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (debug) Debug.Log(string.Format("NOT ENOUGH FUEL - meta2 = {0}, IsOn = {1}",
                                    _blockValue.meta2, BlockGenerator.AnimOn(_blockValue.meta2)));
                                if (BlockGenerator.AnimOn(_blockValue.meta2))
                                {
                                    // if not with animation Off, set it now, I will be using the bit 0.
                                    _blockValue.meta2 = (byte)(_blockValue.meta2 & ~(1 << 0));
                                    _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
                                }
                            }
                            resultado = true;
                        }
                        else
                        {
                            if (debug) Debug.Log(string.Format("NOT FUEL AVAILABLE - meta2 = {0}, IsOn = {1}",
                                _blockValue.meta2, BlockGenerator.AnimOn(_blockValue.meta2)));
                            if (BlockGenerator.AnimOn(_blockValue.meta2))
                            {
                                // if not with animation Off, set it now, I will be using the bit 0.
                                _blockValue.meta2 = (byte)(_blockValue.meta2 & ~(1 << 0));
                                _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
                            }
                        }
                    }
                }
                else
                    if (debug) Debug.Log("No container for this object");
                #endregion;
            }
            else if (debug) Debug.Log("No fuel configured");
            // decay and heat map
            if (resultado)
            {
                // here we add the decay interval
                if (decayValue > 0)
                {
                    // do decay
                    if (DateTime.Now > dtaNextDecay)
                    {
                        dtaNextDecay = DateTime.Now.AddSeconds(decayTime);
                        Block.list[_blockValue.type].DamageBlock(_world, _cIdx, _blockPos, _blockValue,
                            decayValue,
                            tileEntity.entityId, false);
                    }
                }                
            }
        }
        catch (Exception ex1)
        {
            if (debug) Debug.Log(ex1.Message);
        }
        return resultado;
    }
}


public class BlockSmallGenerator : BlockGenerator
{
    private BlockActivationCommand[] OO = new BlockActivationCommand[4]
   {
        new BlockActivationCommand("open", "door", false),
        new BlockActivationCommand("turn on", "lock", false),
        new BlockActivationCommand("turn off", "unlock", false),
        new BlockActivationCommand("pickup", "hand", false)
   };

    private bool debug = false;

    private bool disableDebug = true;

    private DateTime dteNextToolTipDisplayTime;

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

    // just the same as the block generator, but the valves will NOT recognize this one.
    // it should also be pickable
    public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx,
        Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        switch (_indexInBlockActivationCommands)
        {
            case 0:
                return base.OnBlockActivated(_indexInBlockActivationCommands, _world, _cIdx, _blockPos, _blockValue, _player);
            case 1:
                // turn it on - bit 1
                if (!SwitchOn(_blockValue.meta2))
                {
                    _blockValue.meta2 = (byte)(_blockValue.meta2 | (1 << 1));
                    _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
                }
                return false;
            case 2:
                // turn it off - bit 1
                if (SwitchOn(_blockValue.meta2))
                {
                    _blockValue.meta2 = (byte)(_blockValue.meta2 & ~(1 << 1));
                    _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
                }
                return true;
            case 3:
                // check if generator inventory is empty. If not, picks up the stuff
                TileEntityLootContainer secureLootContainer = _world.GetTileEntity(_cIdx, _blockPos) as TileEntityLootContainer;
                if (secureLootContainer != null)
                {
                    bool isModified = false;
                    ItemStack[] items = secureLootContainer.GetItems();
                    if (items != null)
                    {
                        for (int i = 0; i < items.Length; i++)
                        {
                            if (items[i] != null)
                            {
                                if (!items[i].IsEmpty())
                                {
                                    // place the stack on player inventory
                                    (_player as EntityPlayerLocal).AddUIHarvestingItem(items[i].Clone(), false);
                                    (_player as EntityPlayerLocal).bag.AddItem(items[i].Clone());
                                    items[i].Clear();
                                    isModified = true;
                                }
                            }
                        }
                        if (isModified)
                            secureLootContainer.SetModified();
                    }
                }
                ItemStack itemStack = Block.list[_blockValue.type].OnBlockPickedUp(_world, _cIdx, _blockPos, _blockValue, _player.entityId);
                if (!_player.inventory.CanTakeItem(itemStack) && !_player.bag.CanTakeItem(itemStack))
                    return false;
                QuestEventManager.Current.BlockPickedUp(Block.list[_blockValue.type].GetBlockName());
                QuestEventManager.Current.ItemAdded(itemStack);
                _world.GetGameManager().PickupBlockServer(_cIdx, _blockPos, _blockValue, _player.entityId);
                // drains player stamina, from the effort of picking it up
                (_player as EntityPlayerLocal).Stats.Stamina.Value = -1;
                DisplayToolTipText("Uff this thing is heavy, i need to catch my breath!");
                return true;
            default:
                return false;
        }
    }

    public override ItemStack OnBlockPickedUp(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _entityId)
    {
        // apply a stamina decrease buff to the player, when carrying this
        DisplayChatAreaText("PICKING UP SMALL GENERATOR");
        return base.OnBlockPickedUp(_world, _clrIdx, _blockPos, _blockValue, _entityId);
    }

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea, Random _rnd)
    {
        DisplayChatAreaText("PLACED SMALL GENERATOR");
        base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);
    }

    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        this.OO[0].enabled = true;
        this.OO[1].enabled = !BlockGenerator.SwitchOn(_blockValue.meta2);
        this.OO[2].enabled = BlockGenerator.SwitchOn(_blockValue.meta2);
        this.OO[3].enabled = !BlockGenerator.SwitchOn(_blockValue.meta2); // can only pickup if its turned off
        return this.OO;
    }

}