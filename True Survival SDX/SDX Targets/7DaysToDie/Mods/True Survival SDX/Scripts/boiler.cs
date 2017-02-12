using System;
using UnityEngine;
using Random = System.Random;
/// <summary>
/// Custom class for boiler - provides heat power
/// Mortelentus 2016 - v1.0
/// </summary>
public class BlockBoiler : BlockCampfire
{
    /// <summary>
    /// Stores the date and time the tool tip was last displayed
    /// </summary>
    private DateTime dteNextToolTipDisplayTime;

    private bool disableDebug = true;
    // -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Displays text in the chat text area (top left corner)
    /// </summary>
    /// <param name="str">The string to display in the chat text area</param>
    private void DisplayChatAreaText(string str)
    {
        if (!disableDebug)
        {
            str = "BOILER: " + str;
            // Check if the game instance is not null
            if (GameManager.Instance != null)
            {
                // Display the string in the chat text area
                EntityAlive entity = GameManager.Instance.World.GetLocalPlayer();
                GameManager.Instance.GameMessage(EnumGameMessages.Chat, str, entity);
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

    // -----------------------------------------------------------------------------------------------

    public override void OnBlockValueChanged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue, BlockValue _newBlockValue)
    {
        // when a state change from on to off or off to on, it passes the information to nightbors, if any of the same type.
        if (IsCampfireLit(_newBlockValue) && !IsCampfireLit(_oldBlockValue))
        {
            //DisplayToolTipText("turned on");
        }
        else if (!IsCampfireLit(_newBlockValue) && IsCampfireLit(_oldBlockValue))
        {
            //DisplayToolTipText("turned off");
        }
        base.OnBlockValueChanged(_world, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
    }

}