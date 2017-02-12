using System;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// Classes for gas tanks
/// They do NOT produce gas, they need to be manually fed with the fuel object.
/// Mortelentus 2016
/// </summary>
public class BlockGasTankSecure : BlockSecureLoot
{
    private bool disableDebug = true;

    private bool debug = false;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;


    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
            str = "GAZTANKSECURE: " + str;
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

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos,
        EntityAlive _entityFocusing)
    {
        string textAct =  base.GetActivationText(_world, _blockValue, _clrIdx, _blockPos, _entityFocusing);        
        if (!disableDebug)
            textAct = string.Format("{0} (Parent = {1}, Power = {2})", textAct, _blockValue.meta2, _blockValue.meta);
        return textAct;
    }

    public override bool OnBlockActivated(int _indexInBlockActivationCommands, WorldBase _world, int _cIdx, Vector3i _blockPos,
        BlockValue _blockValue, EntityAlive _player)
    {
        // GetPower(_world, _cIdx, _blockPos);
        return base.OnBlockActivated(_indexInBlockActivationCommands, _world, _cIdx, _blockPos, _blockValue, _player);
    }

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue,
        BlockValue _newBlockValue)
    {
        //  checks if it still has some power unit left
        if (_newBlockValue.meta <= 0)
        {
            int newPower = GetPower(_world, _clrIdx, _blockPos, 10); // retrieves as much as 2 batches, if possible
            DisplayChatAreaText(string.Format("Power reached 0! {0} power units added", newPower));
            // if no more power left, it turns off
        }
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
    }

    public int GetPower(WorldBase _world, int _cIdx, Vector3i _blockPos, int powerUnits)
    {
        int resultado = 0;
        string fuelName = "";
        string emptyName = "";
        ItemValue FuelObject = (ItemValue)null;
        ItemValue VesselObject = (ItemValue)null;
        int numberFuelUnits = Convert.ToInt32(Math.Ceiling((double)powerUnits / 5)); // calculates how many fuel units are required for the request each fuel unit gives 5 power units
        #region Find the required objects from config;
        try
        {
            // finds the fuel object
            if (this.Properties.Values.ContainsKey("FuelName"))
            {
                fuelName = this.Properties.Values["FuelName"];
            }
            FuelObject = ItemClass.GetItem(fuelName);
            DisplayChatAreaText("Fuel Object: " + FuelObject.ToString());
            // finds the emtpy object
            if (this.Properties.Values.ContainsKey("EmptyName"))
            {
                emptyName = this.Properties.Values["EmptyName"];
            }
            if (emptyName != "")
            {
                VesselObject = ItemClass.GetItem(emptyName);
                DisplayChatAreaText("Vessel Object: " + FuelObject.ToString());
            }
        }
        catch (Exception ex)
        {
            DisplayChatAreaText("WARNING - no fuel found");
        }
        #endregion;
        if (FuelObject != null)
        {
            // try to find the godamn loot container list
            TileEntitySecureLootContainer Container;
            TileEntity tileEntity = (TileEntity)null;

            tileEntity = _world.GetTileEntity(_cIdx, _blockPos);
            if (tileEntity != null)
            {
                if (tileEntity is TileEntitySecureLootContainer)
                {
                    Container = (TileEntitySecureLootContainer)tileEntity;
                    // finds first with fuel and stack>0
                    // finds first emtpy
                    // finds first with emptyshell if defined.
                    ItemStack fuelStack = (ItemStack)null;
                    ItemStack emtpyStack = (ItemStack)null;
                    ItemStack vesselStack = (ItemStack)null;
                    #region Searches container for required items;
                    foreach (ItemStack itemStack1 in Container.items)
                    {
                        if (itemStack1.itemValue.Equals(FuelObject) && fuelStack == null)
                        {
                            if (itemStack1.count > 0)
                            {
                                fuelStack = itemStack1;
                            }
                        }
                        else if (itemStack1.count == 0 && emtpyStack == null)
                        {
                            emtpyStack = itemStack1;
                        }
                        else if (VesselObject != null)
                        {
                            if (itemStack1.count > 0 && itemStack1.itemValue.Equals(VesselObject) &&
                                vesselStack == null)
                            {
                                vesselStack = itemStack1;
                            }
                        }
                        if (fuelStack != null && (VesselObject != null && vesselStack != null && emtpyStack != null))
                            break;
                    }
                    #endregion;
                    if (fuelStack != null)
                    {
                        #region Transforms the objects accordingly;
                        DisplayChatAreaText(fuelName + " exists count = " + fuelStack.count.ToString());
                        if (fuelStack.count < numberFuelUnits) numberFuelUnits = fuelStack.count; // if not enough units for the full request, takes whatever possible
                        fuelStack.count = fuelStack.count - numberFuelUnits;
                        // adds as much power units (5 ticks of energy) as the number of fuel units consumed
                        BlockValue block = _world.GetBlock(_cIdx, _blockPos);
                        int newPowerValue = block.meta;
                        if (newPowerValue < 0) newPowerValue = 0;
                        newPowerValue = newPowerValue + (numberFuelUnits * 5); // each fuel unit gives 5 power units
                        block.meta = (byte)newPowerValue;
                        _world.SetBlockRPC(_cIdx, _blockPos, block);
                        // converts to empty gazcan?
                        if (VesselObject != null)
                        {
                            if (vesselStack != null)
                            {
                                DisplayChatAreaText("Increasing " + emptyName);
                                vesselStack.count = vesselStack.count + numberFuelUnits;
                            }
                            else if (emtpyStack != null)
                            {
                                DisplayChatAreaText("Creating " + emptyName);
                                emtpyStack.itemValue = VesselObject;
                                emtpyStack.count = numberFuelUnits;
                            }
                        }
                        Container.SetModified();
                        #endregion;
                        resultado = numberFuelUnits;
                    }
                    else
                    {
                        DisplayChatAreaText(string.Format("No {0} available", fuelName));
                    }
                }
            }
            else
                DisplayChatAreaText("No container for this object");
        }
        else DisplayChatAreaText("No fuel configured");
        return resultado;
    }
}
/// <summary>
/// The insecure block does not provide power, it is basically broken
/// </summary>
public class BlockGasTankInSecure : BlockLoot
{
    private bool disableDebug = true;
    private bool debug = false;

    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
            str = "GAZTANKINSECURE: " + str;
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

}