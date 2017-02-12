using UnityEngine;
using System.Collections.Generic;


public class BlockModelTreeEx : BlockModelTree
{
	// Stores the XML parameter name you can use to specify the spacing
	protected static string PropAllowSize = "AllowSize";
	
	// Stores the allowed spacing size
	protected Vector3i allowSize = new Vector3i(3,3,6);
	
	public override void LateInit()
	{
		// Run base code
		base.LateInit();
		
		// Check if the block had the "AllowSize" spacing XML parameter specified
		if(base.Properties.Values.ContainsKey(PropAllowSize))
		{
			// Store the "AllowSize" spacing parameter specified in the XML
			allowSize = Vector3i.Parse(base.Properties.Values[PropAllowSize]);
		}
	}

	public override bool CanPlaceBlockAt(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
	{
		// Check if the block is within the "AllowSize" spacing
		for (int i = _blockPos.x - allowSize.x; i <= (_blockPos.x + allowSize.x); i++)
		{
			for (int j = _blockPos.z - allowSize.z; j <= (_blockPos.z + allowSize.z); j++)
			{
				for (int k = _blockPos.y - allowSize.y; k <= (_blockPos.y + allowSize.y); k++)
				{
					BlockValue block = _world.GetBlock(_clrIdx, new Vector3i(i, k, j));
					if (Block.list[block.type] is BlockModelTree)
					{
						return false;
					}
					if (Block.list[block.type] is BlockModelTreeEx)
					{
						return false;
					}
				}
			}
		}
		return true;
	}
}