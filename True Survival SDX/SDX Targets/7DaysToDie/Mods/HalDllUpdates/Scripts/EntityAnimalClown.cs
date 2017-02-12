using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

public class EntityAnimalClown : EntityZombie
{
    private float meshScale = 1;

    public EntityAnimalClown() : base()
    {

    }

    public override void Init(int _entityClass)
    {
        base.Init(_entityClass);
        EntityClass entityClass = EntityClass.list[_entityClass];
        if (entityClass.Properties.Values.ContainsKey("MeshScale"))
        {
            string meshScaleStr = entityClass.Properties.Values["MeshScale"];
            string[] parts = meshScaleStr.Split(',');

            float minScale = 1;
            float maxScale = 1;

            if (parts.Length == 1)
            {
                maxScale = minScale = float.Parse(parts[0]);
            }
            else if (parts.Length == 2)
            {
                minScale = float.Parse(parts[0]);
                maxScale = float.Parse(parts[1]);
            }

            meshScale = UnityEngine.Random.Range(minScale, maxScale);
            this.gameObject.transform.localScale = new Vector3(meshScale, meshScale, meshScale);
        }
    }

    public override void OnUpdateLive()
    {
        base.OnUpdateLive();

    }

    protected override void Awake()
    {
        base.Awake();
        
    }

    public override int DamageEntity(DamageSource _damageSource, int _strength, bool _criticalHit, float impulseScale)
    {
        
        int ret = base.DamageEntity(_damageSource, _strength, _criticalHit, impulseScale);
        return ret;
    }
    public override bool IsImmuneToLegDamage
    {
        get { return false; }
    }
    protected override DamageResponse damageEntityLocal(DamageSource _damageSource, int _strength, bool _criticalHit, float impulseScale)
    {

        this.Health -= _strength;
        DamageResponse ret =  base.damageEntityLocal(_damageSource, _strength, _criticalHit, impulseScale);
     //   Debug.Log("Response: " + ret.ToString() + " strength: " + _strength + " type: " + _damageSource.GetName().ToString() + " health: " + this.Health);
        return ret;

    }
    public override Vector3 GetMapIconScale()
    {
        return new Vector3(0.45f, 0.45f, 1f);
    }
}

public class EntityBipedZCop : EntityZombieCop
{
    private float meshScale = 1;

    public EntityBipedZCop() : base()
    {

    }

    public override void Init(int _entityClass)
    {
        base.Init(_entityClass);
        EntityClass entityClass = EntityClass.list[_entityClass];
        if (entityClass.Properties.Values.ContainsKey("MeshScale"))
        {
            string meshScaleStr = entityClass.Properties.Values["MeshScale"];
            string[] parts = meshScaleStr.Split(',');

            float minScale = 1;
            float maxScale = 1;

            if (parts.Length == 1)
            {
                maxScale = minScale = float.Parse(parts[0]);
            }
            else if (parts.Length == 2)
            {
                minScale = float.Parse(parts[0]);
                maxScale = float.Parse(parts[1]);
            }

            meshScale = UnityEngine.Random.Range(minScale, maxScale);
            this.gameObject.transform.localScale = new Vector3(meshScale, meshScale, meshScale);
        }
    }

    public override void OnUpdateLive()
    {
        base.OnUpdateLive();

    }

    protected override void Awake()
    {
        base.Awake();

    }

    public override int DamageEntity(DamageSource _damageSource, int _strength, bool _criticalHit, float impulseScale)
    {

        int ret = base.DamageEntity(_damageSource, _strength, _criticalHit, impulseScale);
        return ret;
    }
    public override bool IsImmuneToLegDamage
    {
        get { return false; }
    }
    protected override DamageResponse damageEntityLocal(DamageSource _damageSource, int _strength, bool _criticalHit, float impulseScale)
    {

        this.Health -= _strength;
        DamageResponse ret = base.damageEntityLocal(_damageSource, _strength, _criticalHit, impulseScale);
        //   Debug.Log("Response: " + ret.ToString() + " strength: " + _strength + " type: " + _damageSource.GetName().ToString() + " health: " + this.Health);
        return ret;

    }
    public override Vector3 GetMapIconScale()
    {
        return new Vector3(0.45f, 0.45f, 1f);
    }
}
