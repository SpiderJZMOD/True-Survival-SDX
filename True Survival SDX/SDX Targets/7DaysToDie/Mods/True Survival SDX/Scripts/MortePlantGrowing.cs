using System;
using Random = System.Random;
using UnityEngine;

//public class BlockMortePlantGrowing : BlockPlantGrowing
/// <summary>
/// Custom class for floating blocks
/// Mortelentus 2016 - v1.0
/// </summary>
public class BlockMortePlantGrowing : BlockPlantGrowing
{
    private bool disableDebug = true;
    private int checkrange = 7;
    private float meshScale = 1;
    float minScale = 1;
    float maxScale = 1;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    // -----------------------------------------------------------------------------------------------

    public override void Init()
    {
        base.Init();
        // mesh size
        if (this.Properties.Values.ContainsKey("MeshScale"))
        {
            string meshScaleStr = this.Properties.Values["MeshScale"];
            string[] parts = meshScaleStr.Split(',');
            
            if (parts.Length == 1)
            {
                maxScale = minScale = float.Parse(parts[0]);
            }
            else if (parts.Length == 2)
            {
                minScale = float.Parse(parts[0]);
                maxScale = float.Parse(parts[1]);
            }            
        }
    }

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
            str = "PLANT: " + str;
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

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        // check if it's not raining.        
        //Debug.Log(string.Format("Tick with rainfall = " + WeatherManager.theInstance.GetCurrentRainfallValue().ToString()));
        if (WeatherManager.theInstance.GetCurrentRainfallValue() == 0.0F)
        {
            //Debug.Log(string.Format("Check for water"));
            //if it's not raining, then check if there's any liquid near it
            if (!this.CheckWaterNear(_world, _clrIdx, _blockPos))
            {
                //Debug.Log(string.Format("NO WATER"));
                DisplayChatAreaText("No water near");
                return true;
            }
        }
        return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
    }

    public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
    {
        base.ForceAnimationState(_blockValue, _ebcd);
        if(_ebcd == null || !_ebcd.bHasTransform) return;
        meshScale = UnityEngine.Random.Range(minScale, maxScale);
        _ebcd.transform.localScale = new Vector3(meshScale, meshScale, meshScale);
    }

    public override bool OnEntityCollidedWithBlock(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, Entity _entity)
    {
        if (!_entity.IsAlive()) return false;
        if (_entity is EntityAlive && !(_entity is EntityPlayerLocal) && !(_entity is EntityPlayer))
            this.DamageBlock(_world, _clrIdx, _blockPos, _blockValue, 1, _entity.entityId, false);
        return base.OnEntityCollidedWithBlock(_world, _clrIdx, _blockPos, _blockValue, _entity);
    }

    private bool CheckWaterNear(WorldBase _world, int _clrIdx, Vector3i _blockPos)
    {
        //string blocks = "";
        for (int i = _blockPos.x - checkrange; i <= (_blockPos.x + checkrange); i++)
        {
            for (int j = _blockPos.z - checkrange; j <= (_blockPos.z + checkrange); j++)
            {
                for (int k = _blockPos.y - checkrange; k <= (_blockPos.y + checkrange); k++)
                {
                    BlockValue block = _world.GetBlock(_clrIdx, new Vector3i(i, k, j));
                    //blocks = blocks + block.type + ", ";
                    if (Block.list[block.type].blockMaterial.IsLiquid)
                    {
                        // deplete water randomly - 20% chance
                        System.Random Rand = new System.Random(Guid.NewGuid().GetHashCode());
                        if (Rand.Next(0, 100) < 20)
                        {
                            DisplayChatAreaText("Consume water");
                            Block.list[block.type].DoExchangeAction(_world, new Vector3i(i, k, j), block, "deplete1", 1);
                            BlockLiquidv2.DepleteFromBlock(block, new Vector3i(i, j, k));
                        }
                        return true;
                    }
                }
            }
        }
        return false;
    }
}

public class BlockMortePlantGrown : BlockCropsGrown
{
    private bool disableDebug = true;

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
            str = "PLANT: " + str;
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

    public override bool OnEntityCollidedWithBlock(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, Entity _entity)
    {
        if (!_entity.IsAlive()) return false;
        if (_entity is EntityAlive && !(_entity is EntityPlayerLocal) && !(_entity is EntityPlayer))
            this.DamageBlock(_world, _clrIdx, _blockPos, _blockValue, 1, _entity.entityId, false);
        return base.OnEntityCollidedWithBlock(_world, _clrIdx, _blockPos, _blockValue, _entity);
    }

}

public class BlockMorteTreeGrown : BlockModelTreeEx
{
    private bool disableDebug = true;

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
            str = "PLANT: " + str;
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

    public override bool OnEntityCollidedWithBlock(WorldBase _world, int _clrIdx, Vector3i _blockPos,
        BlockValue _blockValue, Entity _entity)
    {
        if (!_entity.IsAlive()) return false;
        if (_entity is EntityAlive && !(_entity is EntityPlayerLocal) && !(_entity is EntityPlayer))
            this.DamageBlock(_world, _clrIdx, _blockPos, _blockValue, 1, _entity.entityId, false);
        return base.OnEntityCollidedWithBlock(_world, _clrIdx, _blockPos, _blockValue, _entity);
    }
}

public class BlockMorteTreeGrowing : BlockModelTreeEx
{
    private bool disableDebug = true;
    private int checkrange = 7;
    private float meshScale = 1;
    float minScale = 1;
    float maxScale = 1;

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
            str = "PLANT: " + str;
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

    public override void Init()
    {
        base.Init();
        // mesh size
        if (this.Properties.Values.ContainsKey("MeshScale"))
        {
            string meshScaleStr = this.Properties.Values["MeshScale"];
            string[] parts = meshScaleStr.Split(',');

            if (parts.Length == 1)
            {
                maxScale = minScale = float.Parse(parts[0]);
            }
            else if (parts.Length == 2)
            {
                minScale = float.Parse(parts[0]);
                maxScale = float.Parse(parts[1]);
            }
        }
    }

    public override void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
    {
        base.ForceAnimationState(_blockValue, _ebcd);
        if (_ebcd == null || !_ebcd.bHasTransform) return;
        meshScale = UnityEngine.Random.Range(minScale, maxScale);
        _ebcd.transform.localScale = new Vector3(meshScale, meshScale, meshScale);
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick,
        ulong _ticksIfLoaded, Random _rnd)
    {
        //Debug.Log(string.Format("Tick with rainfall = " + WeatherManager.theInstance.GetCurrentRainfallValue().ToString()));
        if (WeatherManager.theInstance.GetCurrentRainfallValue() == 0.0F)
        {
            //Debug.Log(string.Format("Check for water"));
            //if it's not raining, then check if there's any liquid near it
            if (!this.CheckWaterNear(_world, _clrIdx, _blockPos))
            {
                //Debug.Log(string.Format("NO WATER"));
                DisplayChatAreaText("No water near");
                return true;
            }
        }
        return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
        return base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);
    }

    public override bool OnEntityCollidedWithBlock(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, Entity _entity)
    {
        bool doDamage = false;
        if (this.Properties.Values.ContainsKey("doDamage"))
        {
            if (bool.TryParse(this.Properties.Values["doDamage"], out doDamage) == false) doDamage = false;
        }
        if (doDamage)
        {
            if (!_entity.IsAlive()) return false;
            if (_entity is EntityAlive && !(_entity is EntityPlayerLocal) && !(_entity is EntityPlayer))
                this.DamageBlock(_world, _clrIdx, _blockPos, _blockValue, 1, _entity.entityId, false);
        }
        return base.OnEntityCollidedWithBlock(_world, _clrIdx, _blockPos, _blockValue, _entity);
    }

    private bool CheckWaterNear(WorldBase _world, int _clrIdx, Vector3i _blockPos)
    {
        //string blocks = "";
        for (int i = _blockPos.x - checkrange; i <= (_blockPos.x + checkrange); i++)
        {
            for (int j = _blockPos.z - checkrange; j <= (_blockPos.z + checkrange); j++)
            {
                for (int k = _blockPos.y - checkrange; k <= (_blockPos.y + checkrange); k++)
                {
                    BlockValue block = _world.GetBlock(_clrIdx, new Vector3i(i, k, j));
                    //blocks = blocks + block.type + ", ";
                    if (Block.list[block.type].blockMaterial.IsLiquid)
                    {
                        // deplete water
                        System.Random Rand = new System.Random(Guid.NewGuid().GetHashCode());
                        if (Rand.Next(0, 100) < 20)
                        {
                            DisplayChatAreaText("Consume water");
                            Block.list[block.type].DoExchangeAction(_world, new Vector3i(i, k, j), block, "deplete1", 1);
                            BlockLiquidv2.DepleteFromBlock(block, new Vector3i(i, j, k));
                        }
                        return true;
                    }
                }
            }
        }
        return false;
    }
}