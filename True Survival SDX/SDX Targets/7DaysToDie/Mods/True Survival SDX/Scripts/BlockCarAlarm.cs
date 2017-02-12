using UnityEngine;
using System.Collections.Generic;


public class BlockCarAlarmLoot : BlockCarExplodeLoot
{
	// Stores the XML parameters
	protected static string PropAlarmChance = "AlarmChance";
    protected static string PropAlarmSound = "AlarmSound";
    protected static string PropAlarmHeatStr = "AlarmHeatStrength";
    protected static string PropAlarmHeatTime = "AlarmHeatTime";

    // default values to make it easier to apply
    protected float alarmChance = 10.0F;
    protected float HeatMapStrength = 75.0F;
    protected ulong HeatMapWorldTime = 2000;
    protected string alarmSound = "carAlarm";

    public override void LateInit()
	{
		// Run base code
		base.LateInit();
		
		if(base.Properties.Values.ContainsKey(PropAlarmChance))
		{
            alarmChance = Utils.ParseFloat(base.Properties.Values[PropAlarmChance]);
        }
        if (base.Properties.Values.ContainsKey(PropAlarmHeatStr))
        {
            HeatMapStrength = Utils.ParseFloat(base.Properties.Values[PropAlarmHeatStr]);
        }
        if (base.Properties.Values.ContainsKey(PropAlarmHeatTime))
        {
            HeatMapWorldTime = ulong.Parse(base.Properties.Values[PropAlarmHeatTime]) * 10UL;
        }
        if (base.Properties.Values.ContainsKey(PropAlarmSound))
        {
            alarmSound = base.Properties.Values[PropAlarmSound];
        }
    }

	public override bool OnBlockActivated(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        if (alarmChance > 0 && alarmSound != "")
        {
            if (_player.GetRandom().Next(0, 100) < alarmChance)
            {
                // play one shot of the alarm
                Audio.Manager.BroadcastPlay(_blockPos.ToVector3(), alarmSound);
                // add a terrible heatmat
                if (HeatMapStrength > 0 && HeatMapWorldTime > 0)
                {
                    if (GameManager.Instance.World.aiDirector != null)
                    {
                        if (HeatMapStrength > 0 && HeatMapWorldTime > 0)
                        {
                            GameManager.Instance.World.aiDirector.NotifyActivity(
                                EnumAIDirectorChunkEvent.Sound, _blockPos, HeatMapStrength,
                                HeatMapWorldTime);
                        }
                    }
                }
            }
        }
        return base.OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
    }
}