using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityZombieLight : EntityZombie
    {

    public static byte LightThreshold = 10;
    public static float CheckDelay = 1f;

    private float nextCheck = 0;   
    byte lightLevel;

    public override float GetApproachSpeed()
    {
        if (GamePrefs.GetInt(EnumGamePrefs.ZombiesRun) == 1)
        {
            return this.speedApproach * this.Stats.SpeedModifier.Value;
        }
        else
        {
            if (this.world.IsDark() || lightLevel < LightThreshold)
                return this.speedApproachNight * this.Stats.SpeedModifier.Value;
            else
                return this.speedApproach * this.Stats.SpeedModifier.Value;
        }
    }

    public override void OnUpdateLive()
    {
        base.OnUpdateLive();

        if (nextCheck < Time.time)
        {
            nextCheck = Time.time + CheckDelay;
            Vector3i v = new Vector3i(this.position);
            if (v.x < 0) v.x -= 1;
            if (v.z < 0) v.z -= 1;
            lightLevel = GameManager.Instance.World.ChunkClusters[0].GetLight(v, Chunk.LIGHT_TYPE.SUN);
        }

    }

}

