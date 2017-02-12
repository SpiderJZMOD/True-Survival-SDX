using System;
using UnityEngine;

/// <summary>
/// Custom class for animated drawbridge (inherited from BlockDoorSecure)
/// </summary>
public class BlockDrawbridge1 : BlockDoorSecure
{
    // -----------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------
    // Animated Drawbridge v1.0
    // ------------------------
    // by Matite
    // November 2015
    // 7 Days to Die Alpha 12.5
    //
    // This drawbridge door code is inherited from the devs BlockDoorSecure code which allows it to be
    // locked.
    //
    // The override code below specifies where the drawbridge door can be placed.
    //
    // Placement Rules...
    // The block where the door is placed:
    // * must be an air, ground cover or water block
    // * must have two air blocks on either side
    // * must have a solid block directly underneath
    // * must have five air blocks directly above
    //
    // -----------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------

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
        // Check if the game instance is not null
        if (GameManager.Instance != null)
        {
            // Display the string in the chat text area
            EntityAlive entity = GameManager.Instance.World.GetLocalPlayer();
            GameManager.Instance.GameMessage(EnumGameMessages.Chat, str, entity);
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

    /// <summary>
    /// Checks whether the drawbridge block is able to be placed at the chosen position
    /// </summary>
    /// <param name="_world"></param>
    /// <param name="_clrIdx"></param>
    /// <param name="_blockPos"></param>
    /// <param name="_blockValue"></param>
    /// <returns>Returns true if the drawbridge block is allowed to be placed in the chosen position</returns>
    public override bool CanPlaceBlockAt(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        // Get placement position block type (we need to check below if it is a suitable location and not over the top of goldenrod or some other block)
        int type = _world.GetBlock(_clrIdx, _blockPos).type;

        // Check if the block being replaced is not air, not ground cover and not water
        if (type != 0 && !Block.list[type].blockMaterial.IsGroundCover && !Block.list[type].blockMaterial.IsLiquid)
        {
            // Debug
            //DisplayChatAreaText("CanPlaceBlockAt = False -> Not Air, Ground Cover or Water (Type = " + type.ToString() + " - Is Liquid? " + Block.list[type].blockMaterial.IsLiquid.ToString() + ")");

            // Display tool tip text message
            DisplayToolTipText("Sorry, you cannot place the drawbridge here!");

            // Exit here
            return false;
        }

        // Debug
        //DisplayChatAreaText("CanPlaceBlockAt = True -> Air, Ground Cover or Water at placement position");

        // Loop
        for (int i = 1; i < 6; i++)
        {
        	// Check if the block is an air block
        	if (_world.GetBlock(_clrIdx, _blockPos.x, _blockPos.y + i, _blockPos.z).type != 0)
        	{
        		// Debug
            	//DisplayChatAreaText("CanPlaceBlockAt = False -> A Block exists above at Pos 1, 2, 3, 4 or 5");
            
        		// Display tool tip text message
            	DisplayToolTipText("Sorry, you need five air blocks above this position!");
            
        		// Exit here
        		return false;
        	}
        }
        
        // Debug
        //DisplayChatAreaText("CanPlaceBlockAt = True -> No Block above at Pos 1, 2, 3, 4 or 5");

        // Check if the block below at position y - 1 is not solid
        if (!Block.list[_world.GetBlock(_clrIdx, _blockPos.x, _blockPos.y - 1, _blockPos.z).type].shape.IsSolidCube)
        {
            // Debug
            //DisplayChatAreaText("CanPlaceBlockAt = False -> Block below is not solid");

            // Display tool tip text message
            DisplayToolTipText("Sorry, you cannot place the drawbridge here as the block below is not solid!");

            // Exit here
            return false;
        }

        // Debug
        //DisplayChatAreaText("CanPlaceBlockAt = True -> Block below is solid");                    


        // Get the rotation of the block being placed
        byte rotation = _blockValue.rotation;

        // Debug
        //DisplayChatAreaText("CanPlaceBlockAt = Rotation Byte is -> " + rotation.ToString());

        // Check if the drawbridge is rotated north to south or south to north
        if (rotation == 1 || rotation == 3)
        {
            // Door Rotation is North to South or South to North
            // -------------------------------------------------

            // Debug
            //DisplayChatAreaText("CanPlaceBlockAt = Rotation is North to South");

            // Check if block 1 on the east and west side axis is an air block
            if (_world.GetBlock(_clrIdx, _blockPos.x, _blockPos.y, _blockPos.z - 1).type == 0 && _world.GetBlock(_clrIdx, _blockPos.x, _blockPos.y, _blockPos.z + 1).type == 0)
            {
                // Debug
                //DisplayChatAreaText("CanPlaceBlockAt = True -> Air block at Z - 1 and Z + 1");

                // Check if block 2 on the east and west side axis is an air block
                if (_world.GetBlock(_clrIdx, _blockPos.x, _blockPos.y, _blockPos.z - 2).type == 0 && _world.GetBlock(_clrIdx, _blockPos.x, _blockPos.y, _blockPos.z + 2).type == 0)
                {
                    // Debug
                    //DisplayChatAreaText("CanPlaceBlockAt = True -> Air block at Z - 2 and Z + 2");

                    // Alow block placement (exit here and return true)
                    return true;
                }
                else
                {
                    // Debug
                    //DisplayChatAreaText("CanPlaceBlockAt = false -> No air block at Z - 2 and/or Z + 2");

                    // Display tool tip text message
                    DisplayToolTipText("Sorry, you need two air blocks on both sides of the door position!");
                }
            }
            else
            {
                // Debug
                //DisplayChatAreaText("CanPlaceBlockAt = false -> No air block at Z - 1 and/or Z + 1");

                // Display tool tip text message
                DisplayToolTipText("Sorry, you need two air blocks on both sides of the door position!");
            }
        }
        else
        {
            // Door Rotation is East to West or West to East
            // ---------------------------------------------

            // Debug
            //DisplayChatAreaText("CanPlaceBlockAt = Rotation is East to West");

            // Check if block 1 on the east and west side axis is an air block
            if (_world.GetBlock(_clrIdx, _blockPos.x - 1, _blockPos.y, _blockPos.z).type == 0 && _world.GetBlock(_clrIdx, _blockPos.x + 1, _blockPos.y, _blockPos.z).type == 0)
            {
                // Debug
                //DisplayChatAreaText("CanPlaceBlockAt = True -> Air block at X - 1 and X + 1");

                // Check if block 2 on the east and west side axis is an air block
                if (_world.GetBlock(_clrIdx, _blockPos.x - 2, _blockPos.y, _blockPos.z).type == 0 && _world.GetBlock(_clrIdx, _blockPos.x + 2, _blockPos.y, _blockPos.z).type == 0)
                {
                    // Debug
                    //DisplayChatAreaText("CanPlaceBlockAt = True -> Air block at X - 2 and X + 2");

                    // Alow block placement (exit here and return true)
                    return true;
                }
                else
                {
                    // Debug
                    //DisplayChatAreaText("CanPlaceBlockAt = false -> No air block at X - 2 and/or X + 2");

                    // Display tool tip text message
                    DisplayToolTipText("Sorry, you need two air blocks on both sides of the door position!");
                }
            }
            else
            {
                // Debug
                //DisplayChatAreaText("CanPlaceBlockAt = false -> No air block at X - 1 and/or X + 1");

                // Display tool tip text message
                DisplayToolTipText("Sorry, you need two air blocks on both sides of the door position!");
            }

        }       

        // Debug
        //DisplayChatAreaText("CanPlaceBlockAt = False -> Failed a true check");

        // Exit here and return false (the drawbridge block cannot be placed here)
        return false;
    }
}
