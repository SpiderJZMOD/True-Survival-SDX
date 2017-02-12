using System;
using A;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Audio;
using XInputDotNetPure;
using Object = UnityEngine.Object;

public class ItemActionRangedAt : ItemActionRanged
{
    //public new static Dictionary<int, ItemActionRangedAt.WH> F;
    private string SC;
    private DateTime dteNextSoundOff;
    private ItemActionFiringState lastState = ItemActionFiringState.Off;
    private int numOff = 10;

    public string V;
    protected float entityDmgModifier = 1.0F;
    protected float blockDmgModifier = 1.0F;
    protected float fallOffModifier = 1.0F;
    protected float kickbackHip = 0.0f;
    protected float kickbackAim = 0.0f;
    float delayO = 0;
    string soundStartO = "";
    string soundEndO = "";
    string soundRepeatO = "";
    int BulletsPerMagazineO = 0;
    float reloadingTimeO = 0;
    bool autofireO = false;
    public bool needReload = false;
    private bool doDebug = false;
    private void DoDebug(string str)
    {
        if (doDebug) Debug.Log("ATTACHMENTS - > " + str);
    }
    public float SetReloadTime(float currentReload, float timeR, string func)
    {
        if (func == "Replace")
            currentReload = timeR;
        else if (func == "Multiply")
            currentReload = currentReload * timeR;
        else if (func == "Divide")
            currentReload = currentReload / timeR;
        else if (func == "Add")
            currentReload = currentReload + timeR;
        else if (func == "Sub")
            currentReload = currentReload - timeR;
        return currentReload;
    }
    public float setModifier(float currentvalue, float value, string func)
    {
        if (func == "Replace")
            currentvalue = value;
        else if (func == "Multiply")
            currentvalue = currentvalue * value;
        else if (func == "Divide")
            currentvalue = currentvalue / value;
        else if (func == "Add")
            currentvalue = currentvalue + value;
        else if (func == "Sub")
            currentvalue = currentvalue - value;
        if (currentvalue < 0) currentvalue = 0;
        return currentvalue;
    }
    public override void ReadFrom(DynamicProperties _props)
    {
        base.ReadFrom(_props);
        this.SC = !_props.Values.ContainsKey("bullet_material") ? "bullet" : _props.Values["bullet_material"];
        this.kickbackHip = !_props.Values.ContainsKey("kickbackHip")
            ? 0.5f
            : Utils.ParseFloat(_props.Values["kickbackHip"]);
        this.kickbackAim = !_props.Values.ContainsKey("kickbackAim")
            ? 0.2f
            : Utils.ParseFloat(_props.Values["kickbackAim"]);
        entityDmgModifier = 1.0F;
        blockDmgModifier = 1.0F;
        fallOffModifier = 1.0F;
        delayO = this.Delay;
        soundStartO = this.soundStart;
        soundEndO = this.soundEnd;
        soundRepeatO = this.soundRepeat;
        BulletsPerMagazineO = this.BulletsPerMagazine;
        reloadingTimeO = this.reloadingTime;
        autofireO = this.AutoFire;
        needReload = true;
        DoDebug("Ranged action Reloaded properties -> needs to apply modifiers");
    }

    public override void ReloadGun(ItemActionData _actionData, float _warmupTime)
    {        
        try
        {
            ItemActionRangedAt.ItemActionDataRanged actionDataRanged =
                (ItemActionRangedAt.ItemActionDataRanged) _actionData;
            actionDataRanged.reloadWarmupTime = _warmupTime;
            float reloadingTimeMod = this.reloadingTimeO;
            #region search for reload modifiers in atachments;
            if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments != null)
            {
                if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length > 1)
                {
                    // search for reloading time modifier
                    for (int i = 1;
                        i <= (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length - 1);
                        i++)
                    {
                        if (!_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i].IsEmpty())
                        {
                            if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i] !=
                                ItemValue.None)
                            {
                                // an attachment exists, check if it has reloading time propertie
                                ItemClass attach =
                                    ItemClass.GetForId(
                                        _actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i]
                                            .type);
                                try
                                {
                                    if (attach.Properties.Classes.ContainsKey("AttachAction0"))
                                    {
                                        DynamicProperties dynamicProperties = attach.Properties.Classes["AttachAction0"];
                                        if (dynamicProperties.Contains("Reload_time"))
                                        {
                                            #region Reload Time;
                                            string function = "Replace";
                                            if (dynamicProperties.Params1.ContainsKey("Reload_time"))
                                            {
                                                function = dynamicProperties.Params1["Reload_time"];
                                            }
                                            float newReload = Utils.ParseFloat(dynamicProperties.Values["Reload_time"]);
                                            // apply modification
                                            reloadingTimeMod = SetReloadTime(reloadingTimeMod, newReload, function);
                                            #endregion;
                                        }
                                    }
                                }
                                catch {} // just skip this one... need to optimize                                                                
                            }
                        }
                    }
                }
            }
            #endregion;
            DoDebug("Reload time = " + reloadingTimeMod);
            this.reloadingTime = reloadingTimeMod;
            base.ReloadGun(_actionData, _warmupTime);
            RestoreDefault();
        }
        catch (Exception ex)
        {
            DoDebug(string.Format("ERROR ReloadGun - {0}", ex.Message));
        }
    }


    protected new float getDamageBlock(ItemActionRanged.ItemActionDataRanged _actionData)
    {
        float blockDmgModifierMod = 1.0F;
        try
        {
            #region Get modifiers from attachments;
            if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments != null)
            {
                if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length > 1)
                {
                    // search for reloading time modifier
                    for (int i = 1;
                        i <= (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length - 1);
                        i++)
                    {
                        if (!_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i].IsEmpty())
                        {
                            if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i] !=
                                ItemValue.None)
                            {
                                // an attachment exists, check if it has reloading time propertie
                                ItemClass attach =
                                    ItemClass.GetForId(
                                        _actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i]
                                            .type);
                                try
                                {
                                    if (attach.Properties.Classes.ContainsKey("AttachAction0"))
                                    {
                                        DynamicProperties dynamicProperties = attach.Properties.Classes["AttachAction0"];
                                        if (dynamicProperties.Contains("Block_Damage_Modifier"))
                                        {
                                            #region Block Damage multiplier;

                                            string function = "Replace";
                                            if (dynamicProperties.Params1.ContainsKey("Block_Damage_Modifier"))
                                            {
                                                function = dynamicProperties.Params1["Block_Damage_Modifier"];
                                            }
                                            float newModifier =
                                                Utils.ParseFloat(dynamicProperties.Values["Block_Damage_Modifier"]);
                                            blockDmgModifierMod = setModifier(blockDmgModifierMod, newModifier, function);

                                            #endregion;
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
            }
            #endregion;
        }
        catch { }
        float dmgBlk = 0;
        if (!ItemClass.GetItemClass(this.MagazineItemNames[(int)_actionData.invData.itemValue.SelectedAmmoTypeIndex]).HasAttributes)
            dmgBlk = this.GetDamageBlock(_actionData.invData.itemValue, _actionData.invData.holdingEntity as EntityPlayer) * blockDmgModifierMod;
        {
            ItemValue _itemValue =
                ItemClass.GetItem(this.MagazineItemNames[(int)_actionData.invData.itemValue.SelectedAmmoTypeIndex]);
            if (!_actionData.invData.item.HasQuality)
            {
                if (!_actionData.invData.item.HasParts)
                    dmgBlk =
                        this.GetDamageBlock(_actionData.invData.itemValue,
                            _actionData.invData.holdingEntity as EntityPlayer) * blockDmgModifierMod;
            }
            if (dmgBlk == 0)
            {
                _itemValue.Quality = _actionData.invData.itemValue.Quality;
                dmgBlk =
                    this.GetDamageBlock(_actionData.invData.itemValue, _actionData.invData.holdingEntity as EntityPlayer) +
                    this.getProjectileDamageBlock(_itemValue, (EntityPlayer)null);
                dmgBlk = dmgBlk * blockDmgModifierMod;
                if ((double)dmgBlk < 0.0)
                    dmgBlk = 0.0f;
            }
        }
        DoDebug(string.Format("BLOCK DMG:{0}", dmgBlk.ToString("#.##")));
        return dmgBlk;
    }

    protected new float getDamageEntity(ItemActionRanged.ItemActionDataRanged _actionData)
    {
        float entityDmgModifierMod = 1.0F;
        try
        {
            #region Get modifiers from attachments;
            if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments != null)
            {
                if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length > 1)
                {
                    // search for reloading time modifier
                    for (int i = 1;
                        i <= (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length - 1);
                        i++)
                    {
                        if (!_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i].IsEmpty())
                        {
                            if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i] !=
                                ItemValue.None)
                            {
                                // an attachment exists, check if it has reloading time propertie
                                ItemClass attach =
                                    ItemClass.GetForId(
                                        _actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i]
                                            .type);
                                try
                                {
                                    if (attach.Properties.Classes.ContainsKey("AttachAction0"))
                                    {
                                        DynamicProperties dynamicProperties = attach.Properties.Classes["AttachAction0"];
                                        if (dynamicProperties.Contains("Entity_Damage_Modifier"))
                                        {
                                            #region Entity damage multiplier

                                            string function = "Replace";
                                            if (dynamicProperties.Params1.ContainsKey("Entity_Damage_Modifier"))
                                            {
                                                function = dynamicProperties.Params1["Entity_Damage_Modifier"];
                                            }
                                            float newModifier =
                                                Utils.ParseFloat(dynamicProperties.Values["Entity_Damage_Modifier"]);
                                            entityDmgModifierMod = setModifier(entityDmgModifierMod, newModifier,
                                                function);

                                            #endregion;
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
            }
            #endregion;
        }
        catch { }
        float dmEnt = base.getDamageBlock(_actionData) * entityDmgModifierMod;

        if (
            !ItemClass.GetItemClass(this.MagazineItemNames[(int)_actionData.invData.itemValue.SelectedAmmoTypeIndex])
                .HasAttributes)
            dmEnt =
                this.GetDamageEntity(_actionData.invData.itemValue, _actionData.invData.holdingEntity as EntityPlayer) *
                entityDmgModifierMod;
        else
        {
            ItemValue _itemValue =
                ItemClass.GetItem(this.MagazineItemNames[(int)_actionData.invData.itemValue.SelectedAmmoTypeIndex]);
            if (!_actionData.invData.item.HasQuality)
            {
                if (!_actionData.invData.item.HasParts)
                    dmEnt =
                        this.GetDamageEntity(_actionData.invData.itemValue,
                            _actionData.invData.holdingEntity as EntityPlayer) * entityDmgModifierMod;
            }
            if (dmEnt == 0)
            {
                _itemValue.Quality = _actionData.invData.itemValue.Quality;
                dmEnt =
                    this.GetDamageEntity(_actionData.invData.itemValue,
                        _actionData.invData.holdingEntity as EntityPlayer) +
                    this.getProjectileDamageEntity(_itemValue, _actionData.invData.holdingEntity as EntityPlayer);
                dmEnt = dmEnt * entityDmgModifierMod;
            }
        }
        if ((double)dmEnt < 0.0)
        {
            dmEnt = 0.0f;
        }
        DoDebug(string.Format("ENTITY DMG:{0}", dmEnt.ToString("#.##")));
        return dmEnt;
    }

    protected override float getRange(ItemValue _itemValue)
    {
        float fallOffModifierMod = 1.0F;

        try
        {
            #region Get modifiers from attachments;
            if (_itemValue.Attachments != null)
            {
                if (_itemValue.Attachments.Length > 1)
                {
                    // search for reloading time modifier
                    for (int i = 1;
                        i <= (_itemValue.Attachments.Length - 1);
                        i++)
                    {
                        if (!_itemValue.Attachments[i].IsEmpty())
                        {
                            if (_itemValue.Attachments[i] !=
                                ItemValue.None)
                            {
                                // an attachment exists, check if it has reloading time propertie
                                ItemClass attach =
                                    ItemClass.GetForId(
                                        _itemValue.Attachments[i]
                                            .type);
                                try
                                {
                                    if (attach.Properties.Classes.ContainsKey("AttachAction0"))
                                    {
                                        DynamicProperties dynamicProperties = attach.Properties.Classes["AttachAction0"];
                                        if (dynamicProperties.Contains("FallOff_Range_Modifier"))
                                        {
                                            #region Falloff Range Modifier;

                                            string function = "Replace";
                                            if (dynamicProperties.Params1.ContainsKey("FallOff_Range_Modifier"))
                                            {
                                                function = dynamicProperties.Params1["FallOff_Range_Modifier"];
                                            }
                                            float newModifier =
                                                Utils.ParseFloat(dynamicProperties.Values["FallOff_Range_Modifier"]);
                                            fallOffModifierMod = setModifier(fallOffModifierMod, newModifier, function);

                                            #endregion;
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
            }
            #endregion;
        }
        catch { }
        float rangeMod = AttributeBase.GetVal<AttributeFalloffRange>(_itemValue, this.Range) * fallOffModifierMod;
        DoDebug(string.Format("RANGE:{0}", rangeMod.ToString("#.##")));
        return rangeMod;
    }

    protected override Vector3 fireShot(int _shotIdx, ItemActionDataRanged _actionData)
    {
        ItemActionRanged.ItemActionDataRanged actionDataRanged = _actionData;
        EntityAlive entityAlive = _actionData.invData.holdingEntity;
        Vector3 _altLookVector;
        if ((UnityEngine.Object)actionDataRanged.muzzle != (UnityEngine.Object)null)
        {
            label_1:
            switch (1)
            {
                case 0:
                    goto label_1;
                default:
                    _altLookVector = actionDataRanged.muzzle.forward;
                    break;
            }
        }
        else
            _altLookVector = Vector3.zero;
        Vector3 direction = entityAlive.GetLookVector(_altLookVector);
        Vector3 vector3_1;
        if ((UnityEngine.Object)actionDataRanged.muzzle != (UnityEngine.Object)null)
        {
            label_7:
            switch (3)
            {
                case 0:
                    goto label_7;
                default:
                    vector3_1 = actionDataRanged.muzzle.position;
                    break;
            }
        }
        else
            vector3_1 = Vector3.zero;
        Vector3 vector3_2 = vector3_1;
        float num1 = this.RaysSpread;
        if (this.MagazineItemRayCount != null && (int)_actionData.invData.itemValue.SelectedAmmoTypeIndex <= this.MagazineItemSpread.Length)
            num1 = this.MagazineItemSpread[(int)_actionData.invData.itemValue.SelectedAmmoTypeIndex];
        if (_actionData.invData.holdingEntity is EntityPlayerLocal)
        {
            label_13:
            switch (2)
            {
                case 0:
                    goto label_13;
                default:
                    EntityPlayerLocal entityPlayerLocal = (EntityPlayerLocal)_actionData.invData.holdingEntity;
                    float offsetPosition = this.getOffsetPosition(_actionData.invData.itemValue, false);
                    if ((double)num1 != 0.0)
                    {
                        vector3_2 = entityPlayerLocal.GetCrosshairPosition3D(0.0f, offsetPosition, vector3_2);
                        direction = entityPlayerLocal.GetShotStartPositionRandom(0.01f, offsetPosition, vector3_2) + (1f - num1) * direction.normalized - vector3_2;
                        break;
                    }
                    vector3_2 = _actionData.invData.holdingEntity.AimingGun ? entityPlayerLocal.GetCrosshairPosition3D(0.0f, offsetPosition, vector3_2) : entityPlayerLocal.GetShotStartPositionRandom(0.01f, offsetPosition, vector3_2);
                    break;
            }
        }
        this.Range = this.getRange(_actionData.invData.itemValue);
        Ray ray = new Ray(vector3_2, direction);
        int num2;
        if (this.hitmaskOverride == 0)
        {
            label_18:
            switch (6)
            {
                case 0:
                    goto label_18;
                default:
                    num2 = 8;
                    break;
            }
        }
        else
            num2 = this.hitmaskOverride;
        int _hitMask = num2;
        if (Voxel.Raycast(_actionData.invData.world, ray, this.Range, -1486853, _hitMask, 0.0f))
        {
            label_22:
            switch (2)
            {
                case 0:
                    goto label_22;
                default:
                    WorldRayHitInfo worldRayHitInfo = Voxel.voxelRayHitInfo.Clone();
                    if ((double)worldRayHitInfo.hit.distanceSq > (double)this.Range * (double)this.Range)
                        return direction;
                    ItemValue itemValue = _actionData.invData.itemValue;
                    float num3 = 1f;
                    if (_actionData.invData.itemValue.MaxUseTimes > 0)
                    {
                        label_26:
                        switch (7)
                        {
                            case 0:
                                goto label_26;
                            default:
                                num3 = (float)(itemValue.MaxUseTimes - itemValue.UseTimes) / (float)itemValue.MaxUseTimes;
                                break;
                        }
                    }
                    World _world = actionDataRanged.invData.world;
                    WorldRayHitInfo hitInfo = worldRayHitInfo;
                    int _attackerEntityId = actionDataRanged.invData.holdingEntity.entityId;
                    int num4;
                    if (this.DamageType == EnumDamageSourceType.Undef)
                    {
                        label_29:
                        switch (4)
                        {
                            case 0:
                                goto label_29;
                            default:
                                num4 = 1;
                                break;
                        }
                    }
                    else
                        num4 = (int)this.DamageType;
                    double num5 = (double)this.getDamageBlock(_actionData);
                    double num6 = (double)this.getDamageEntity(_actionData);
                    double num7 = 1.0;
                    double num8 = (double)num3;
                    double num9 = (double)_actionData.invData.item.CritChance.Value;
                    double num10 = (double)this.getDismembermentBaseChance((ItemActionData)_actionData);
                    double num11 = (double)this.getDismembermentBonus((ItemActionData)_actionData);
                    string _attackingDeviceMadeOf = this.SC;
                    DamageMultiplier _damageMultiplier = this.damageMultiplier;
                    List<MultiBuffClassAction> buffActions = this.getBuffActions((ItemActionData)_actionData);
                    ItemActionAttack.AttackHitInfo _attackDetails = actionDataRanged.attackDetails;
                    int actionExp = this.ActionExp;
                    double num12 = (double)this.ActionExpBonusMultiplier;
                    // ISSUE: variable of the null type
                    ItemActionAttack local1 = null;
                    // ISSUE: variable of the null type
                    Dictionary<string, float> local2 = null;
                    int num13 = 0;
                    ItemActionAttack.Hit(_world, hitInfo, _attackerEntityId, (EnumDamageSourceType)num4, (float)num5, (float)num6, (float)num7, (float)num8, (float)num9, (float)num10, (float)num11, _attackingDeviceMadeOf, _damageMultiplier, buffActions, _attackDetails, actionExp, (float)num12, (ItemActionAttack)local1, (Dictionary<string, float>)local2, (ItemActionAttack.EnumAttackMode)num13);
                    if (this.bSupportHarvesting)
                    {
                        GameUtils.HarvestOnAttack((ItemActionAttackData)_actionData, (Dictionary<string, float>)null);
                        break;
                    }
                    break;
            }
        }
        return direction;
    }

    public override void ExecuteAction(ItemActionData _actionData, bool _bReleased)
    {
        try
        {
            bool autofire = this.autofireO;
            float delayMod = this.delayO;
            try
            {
                #region Get modifiers from attachments;
                if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments != null)
                {
                    if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length > 1)
                    {
                        // search for reloading time modifier
                        for (int i = 1;
                            i <= (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length - 1);
                            i++)
                        {
                            if (!_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i].IsEmpty())
                            {
                                if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i] !=
                                    ItemValue.None)
                                {
                                    // an attachment exists, check if it has reloading time propertie
                                    ItemClass attach =
                                        ItemClass.GetForId(
                                            _actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i]
                                                .type);
                                    try
                                    {
                                        if (attach.Properties.Classes.ContainsKey("AttachAction0"))
                                        {
                                            DynamicProperties dynamicProperties = attach.Properties.Classes["AttachAction0"];
                                            if (dynamicProperties.Contains("Auto_Fire"))
                                            {
                                                if (bool.TryParse(dynamicProperties.Values["Auto_Fire"], out autofire) == false) autofire = false;
                                            }
                                            if (dynamicProperties.Contains("Delay"))
                                            {
                                                #region Delay;
                                                // DisplayChatAreaText(string.Format("Setting Magazine_size={0}", dynamicProperties.Values["Magazine_size"])); 
                                                string function = "Replace";
                                                if (dynamicProperties.Params1.ContainsKey("Delay"))
                                                {
                                                    function = dynamicProperties.Params1["Delay"];
                                                }
                                                // calculate the new value according to the selected function                                                                                        
                                                if (function == "Replace")
                                                    delayMod =
                                                        System.Convert.ToInt32(
                                                            dynamicProperties.Values["Delay"]);
                                                else if (function == "Add")
                                                    delayMod +=
                                                        System.Convert.ToInt32(
                                                            dynamicProperties.Values["Delay"]);
                                                else if (function == "Sub")
                                                    delayMod -=
                                                        System.Convert.ToInt32(
                                                            dynamicProperties.Values["Delay"]);
                                                else if (function == "Multiply")
                                                {
                                                    float multiplier =
                                                        Utils.ParseFloat(
                                                            dynamicProperties.Values["Delay"]);
                                                    delayMod =
                                                        System.Convert.ToInt32(
                                                            System.Math.Round(multiplier * delayMod));
                                                }
                                                if (delayMod <= 0) delayMod = 1;
                                                #endregion;
                                            }
                                        }
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion;
            }
            catch { }
            this.AutoFire = autofire;
            this.Delay = delayMod;
            base.ExecuteAction(_actionData, _bReleased);
            RestoreDefault();
        }
        catch (Exception ex)
        {
            DoDebug(string.Format("ERROR ExecuteAction - {0}", ex.Message));
        }
    }

    private void RestoreDefault()
    {
        this.Delay = delayO;
        this.soundStart = soundStartO;
        this.soundEnd = soundEndO;
        this.soundRepeat = soundRepeatO;
        this.BulletsPerMagazine = BulletsPerMagazineO;
        this.reloadingTime = reloadingTimeO;
        this.AutoFire = autofireO;
    }

    public override bool CanReload(ItemActionData _actionData)
    {
        ItemValue holdingItemItemValue = _actionData.invData.holdingEntity.inventory.holdingItemItemValue;
        ItemActionRanged.ItemActionDataRanged actionDataRanged = (ItemActionRanged.ItemActionDataRanged) _actionData;
        ItemValue _itemValue =
            ItemClass.GetItem(this.MagazineItemNames[(int) holdingItemItemValue.SelectedAmmoTypeIndex]);
        // get the modded bullets per magazine.
        int BulletsPerMagazineMod = this.BulletsPerMagazineO;
        try
        {
            #region Modify BulletsPerMagazine;

            if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments != null)
            {
                if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length > 1)
                {
                    // search for reloading time modifier
                    for (int i = 1;
                        i <=
                        (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length -
                         1);
                        i++)
                    {
                        if (!_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i]
                            .IsEmpty())
                        {
                            if (
                                _actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments
                                    [i] !=
                                ItemValue.None)
                            {
                                // an attachment exists, check if it has reloading time propertie
                                ItemClass attach =
                                    ItemClass.GetForId(
                                        _actionData.invData.holdingEntity.inventory.holdingItemItemValue
                                            .Attachments[i].type);
                                try
                                {
                                    if (attach.Properties.Classes.ContainsKey("AttachAction0"))
                                    {
                                        DynamicProperties dynamicProperties =
                                            attach.Properties.Classes["AttachAction0"];
                                        if (dynamicProperties.Contains("Magazine_size"))
                                        {
                                            #region Magazine Size;

                                            // DisplayChatAreaText(string.Format("Setting Magazine_size={0}", dynamicProperties.Values["Magazine_size"])); 
                                            string function = "Replace";
                                            if (dynamicProperties.Params1.ContainsKey("Magazine_size"))
                                            {
                                                function = dynamicProperties.Params1["Magazine_size"];
                                            }
                                            // calculate the new value according to the selected function                                                                                        
                                            if (function == "Replace")
                                                BulletsPerMagazineMod =
                                                    System.Convert.ToInt32(
                                                        dynamicProperties.Values["Magazine_size"]);
                                            else if (function == "Add")
                                                BulletsPerMagazineMod +=
                                                    System.Convert.ToInt32(
                                                        dynamicProperties.Values["Magazine_size"]);
                                            else if (function == "Sub")
                                                BulletsPerMagazineMod -=
                                                    System.Convert.ToInt32(
                                                        dynamicProperties.Values["Magazine_size"]);
                                            else if (function == "Multiply")
                                            {
                                                float multiplier =
                                                    Utils.ParseFloat(
                                                        dynamicProperties.Values["Magazine_size"]);
                                                BulletsPerMagazineMod =
                                                    System.Convert.ToInt32(
                                                        System.Math.Round(multiplier*BulletsPerMagazineMod));
                                            }
                                            if (BulletsPerMagazineMod <= 0) BulletsPerMagazineMod = 1;

                                            #endregion;
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
            }

            #endregion;
        }
        catch
        {
        }
        this.BulletsPerMagazine = BulletsPerMagazineMod;
        return base.CanReload(_actionData);
        RestoreDefault();
    }
    public override void ItemActionEffects(GameManager _gameManager, ItemActionData _actionData, int _firingState, Vector3 _startPos, Vector3 _direction)
    {
        string pos = "1";
        try
        {
            GetAllMods(_actionData);
            float kickbackHipMod = kickbackHip;
            float kickbackAimMod = kickbackAim;
            pos = "3";
            // for now, only works to "amplify" the kickback
            // so you can define strong kickbacks for unmodified weapons
            // and add attachments to reduce it to vanilla minimum
            // search for reload modifiers in atachments
            #region Get kickback modifiers;
            if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments != null)
            {
                if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length > 1)
                {
                    // search for reloading time modifier
                    for (int i = 1;
                        i <= (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length - 1);
                        i++)
                    {
                        if (!_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i].IsEmpty())
                        {
                            if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i] !=
                                ItemValue.None)
                            {
                                // an attachment exists, check if it has reloading time propertie
                                ItemClass attach =
                                    ItemClass.GetForId(
                                        _actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i]
                                            .type);
                                try
                                {
                                    if (attach.Properties.Classes.ContainsKey("AttachAction0"))
                                    {
                                        DynamicProperties dynamicProperties = attach.Properties.Classes["AttachAction0"];
                                        if (dynamicProperties.Contains("kickbackAim"))
                                        {
                                            #region kickbackAim multiplier

                                            string function = "Replace";
                                            if (dynamicProperties.Params1.ContainsKey("kickbackAim"))
                                            {
                                                function = dynamicProperties.Params1["kickbackAim"];
                                            }
                                            float newModifier = Utils.ParseFloat(dynamicProperties.Values["kickbackAim"]);
                                            kickbackAimMod = setModifier(kickbackAimMod, newModifier, function);

                                            #endregion;                                
                                        }
                                        if (dynamicProperties.Contains("kickbackHip"))
                                        {
                                            #region kickbackHip multiplier

                                            string function = "Replace";
                                            if (dynamicProperties.Params1.ContainsKey("kickbackHip"))
                                            {
                                                function = dynamicProperties.Params1["kickbackHip"];
                                            }
                                            float newModifier = Utils.ParseFloat(dynamicProperties.Values["kickbackHip"]);
                                            kickbackHipMod = setModifier(kickbackHipMod, newModifier, function);

                                            #endregion;

                                        }                                        
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
            }
            #endregion;
            pos = "4";
            base.ItemActionEffects(_gameManager, _actionData, _firingState, _startPos, _direction);
            pos = "4";
            ItemActionFiringState actionFiringState = (ItemActionFiringState) _firingState;
            pos = "5";
            try
            {
                if (kickbackHipMod > 0 || kickbackAimMod > 0)
                    if (actionFiringState == ItemActionFiringState.Loop || actionFiringState == ItemActionFiringState.Start)
                    {
                        DoKickBack(_actionData, kickbackHipMod, kickbackAimMod);
                    }
            }
            catch{}            
            pos = "6";
        }
        catch (Exception ex)
        {
            DoDebug(string.Format("ERROR ItemActionEffects (pos={1}) - {0}", ex.Message, pos));
            base.ItemActionEffects(_gameManager, _actionData, _firingState, _startPos, _direction);
        }
    }
    private void DoKickBack(ItemActionData _actionData, float kickBackHipMod, float kickBackAimMod)
    {
        EntityPlayerLocal plr = (_actionData.invData.holdingEntity as EntityPlayerLocal);
        // no shake, since it was already applied by vanilla
        //switch (plr.inventory.holdingItem.GetCameraShakeType(plr.inventory.holdingItemData))
        //{
        //    case EnumCameraShake.Tiny:
        //        plr.cameraTransform.SendMessage("ShakeTiny");
        //        break;
        //    case EnumCameraShake.Small:
        //        plr.cameraTransform.SendMessage("Shake");
        //        break;
        //    case EnumCameraShake.Big:
        //        plr.cameraTransform.SendMessage("ShakeBig");
        //        break;
        //}
        if (!plr.inventory.holdingItem.bCrosshairUpAfterShot || !plr.bFirstPersonView)
            return;
        if (!plr.AimingGun)
        {
            DoDebug(string.Format("Aditional KickbackHip of {0}", kickBackHipMod.ToString("#.##")));
            plr.movementInput.rotation.x += kickBackHipMod;
        }
        else
        {
            DoDebug(string.Format("Aditional KickbackAim of {0}", kickBackAimMod.ToString("#.##")));
            plr.movementInput.rotation.x += kickBackAimMod;
        }
    }
    // get all modifiers
    private void GetAllMods(ItemActionData _actionData)
    {
        int BulletsPerMagazineMod = this.BulletsPerMagazineO;
        float delayMod = this.delayO;
        string soundStartMod = this.soundStartO;
        string soundEndMod = this.soundEndO;
        string soundRepeatMod = this.soundRepeatO;
        bool autofire = this.autofireO;
        try
        {
            #region Modify BulletsPerMagazine;

            if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments != null)
            {
                if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length > 1)
                {
                    // search for reloading time modifier
                    for (int i = 1;
                        i <=
                        (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length -
                         1);
                        i++)
                    {
                        if (!_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i]
                                .IsEmpty())
                        {
                            if (
                                _actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments
                                    [i] !=
                                ItemValue.None)
                            {
                                // an attachment exists, check if it has reloading time propertie
                                ItemClass attach =
                                    ItemClass.GetForId(
                                        _actionData.invData.holdingEntity.inventory.holdingItemItemValue
                                            .Attachments[i].type);
                                try
                                {
                                    if (attach.Properties.Classes.ContainsKey("AttachAction0"))
                                    {
                                        DynamicProperties dynamicProperties =
                                            attach.Properties.Classes["AttachAction0"];
                                        if (dynamicProperties.Contains("Magazine_size"))
                                        {
                                            #region Magazine Size;

                                            // DisplayChatAreaText(string.Format("Setting Magazine_size={0}", dynamicProperties.Values["Magazine_size"])); 
                                            string function = "Replace";
                                            if (dynamicProperties.Params1.ContainsKey("Magazine_size"))
                                            {
                                                function = dynamicProperties.Params1["Magazine_size"];
                                            }
                                            // calculate the new value according to the selected function                                                                                        
                                            if (function == "Replace")
                                                BulletsPerMagazineMod =
                                                    System.Convert.ToInt32(
                                                        dynamicProperties.Values["Magazine_size"]);
                                            else if (function == "Add")
                                                BulletsPerMagazineMod +=
                                                    System.Convert.ToInt32(
                                                        dynamicProperties.Values["Magazine_size"]);
                                            else if (function == "Sub")
                                                BulletsPerMagazineMod -=
                                                    System.Convert.ToInt32(
                                                        dynamicProperties.Values["Magazine_size"]);
                                            else if (function == "Multiply")
                                            {
                                                float multiplier =
                                                    Utils.ParseFloat(
                                                        dynamicProperties.Values["Magazine_size"]);
                                                BulletsPerMagazineMod =
                                                    System.Convert.ToInt32(
                                                        System.Math.Round(multiplier * BulletsPerMagazineMod));
                                            }
                                            if (BulletsPerMagazineMod <= 0) BulletsPerMagazineMod = 1;

                                            #endregion;
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
            }

            #endregion;
            #region Get sounds, and try to shutdown remotely;            
            if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments != null)
            {
                if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length > 1)
                {
                    // search for reloading time modifier
                    for (int i = 1;
                        i <= (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length - 1);
                        i++)
                    {
                        if (!_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i].IsEmpty())
                        {
                            if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i] !=
                                ItemValue.None)
                            {
                                // an attachment exists, check if it has reloading time propertie
                                ItemClass attach =
                                    ItemClass.GetForId(
                                        _actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i]
                                            .type);
                                try
                                {
                                    if (attach.Properties.Classes.ContainsKey("AttachAction0"))
                                    {
                                        DynamicProperties dynamicProperties = attach.Properties.Classes["AttachAction0"];
                                        if (dynamicProperties.Contains("Sound_start"))
                                        {
                                            #region Sound Start;                     

                                            soundStartMod = dynamicProperties.Values["Sound_start"];

                                            #endregion;
                                        }
                                        if (dynamicProperties.Contains("Sound_repeat"))
                                        {
                                            #region Sound Repeat;                                

                                            soundRepeatMod = dynamicProperties.Values["Sound_repeat"];

                                            #endregion;
                                        }
                                        if (dynamicProperties.Contains("Sound_end"))
                                        {
                                            #region Sound End;                                

                                            soundEndMod = dynamicProperties.Values["Sound_end"];

                                            #endregion;
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
            }
            #endregion;
            #region Modify Delay and autofire;

            if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments != null)
            {
                if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length > 1)
                {
                    // search for reloading time modifier
                    for (int i = 1;
                        i <=
                        (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length -
                         1);
                        i++)
                    {
                        if (!_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i]
                                .IsEmpty())
                        {
                            if (
                                _actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments
                                    [i] !=
                                ItemValue.None)
                            {
                                // an attachment exists, check if it has reloading time propertie
                                ItemClass attach =
                                    ItemClass.GetForId(
                                        _actionData.invData.holdingEntity.inventory.holdingItemItemValue
                                            .Attachments[i].type);
                                try
                                {
                                    if (attach.Properties.Classes.ContainsKey("AttachAction0"))
                                    {
                                        DynamicProperties dynamicProperties =
                                            attach.Properties.Classes["AttachAction0"];
                                        if (dynamicProperties.Contains("Auto_Fire"))
                                        {
                                            if (bool.TryParse(dynamicProperties.Values["Auto_Fire"], out autofire) == false) autofire = false;
                                        }
                                        if (dynamicProperties.Contains("Delay"))
                                        {
                                            #region Delay;
                                            // DisplayChatAreaText(string.Format("Setting Magazine_size={0}", dynamicProperties.Values["Magazine_size"])); 
                                            string function = "Replace";
                                            if (dynamicProperties.Params1.ContainsKey("Delay"))
                                            {
                                                function = dynamicProperties.Params1["Delay"];
                                            }
                                            // calculate the new value according to the selected function                                                                                        
                                            if (function == "Replace")
                                                delayMod =
                                                    System.Convert.ToInt32(
                                                        dynamicProperties.Values["Delay"]);
                                            else if (function == "Add")
                                                delayMod +=
                                                    System.Convert.ToInt32(
                                                        dynamicProperties.Values["Delay"]);
                                            else if (function == "Sub")
                                                delayMod -=
                                                    System.Convert.ToInt32(
                                                        dynamicProperties.Values["Delay"]);
                                            else if (function == "Multiply")
                                            {
                                                float multiplier =
                                                    Utils.ParseFloat(
                                                        dynamicProperties.Values["Delay"]);
                                                delayMod =
                                                    System.Convert.ToInt32(
                                                        System.Math.Round(multiplier * delayMod));
                                            }
                                            if (delayMod <= 0) delayMod = 1;
                                            #endregion;
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
            }

            #endregion;
        }
        catch { }
        this.soundStart = soundStartMod;
        this.soundEnd = soundEndMod;
        this.soundRepeat = soundRepeatMod;
        this.BulletsPerMagazine = BulletsPerMagazineMod;
        this.Delay = delayMod;
        this.AutoFire = autofire;
    }


    // this is needed because of modifiers, particularly the magazine size
    public override void OnHoldingUpdate(ItemActionData _actionData)
    {
        GetAllMods(_actionData);
        ItemActionRangedAt.ItemActionDataRanged actionDataRanged = (ItemActionRangedAt.ItemActionDataRanged)_actionData;

        if (actionDataRanged.state == ItemActionFiringState.Off)
        {
            if (lastState != actionDataRanged.state) numOff = 0;
            if (numOff < 2)
            {
                numOff++;
                try
                {
                    Audio.Manager.Stop(_actionData.invData.holdingEntity.entityId, this.soundStart);
                    Audio.Manager.Stop(_actionData.invData.holdingEntity.entityId, this.soundRepeat);
                    Audio.Manager.Stop(_actionData.invData.holdingEntity.entityId, this.soundEnd);
                }
                catch (Exception)
                {                    
                }                
            }
        }
        lastState = actionDataRanged.state;
        base.OnHoldingUpdate(_actionData);
    }

    //public class WH
    //{
    //    public int E;
    //    public int I;
    //    public int O;

    //    public WH()
    //    {
    //        this.E = -1;
    //        this.I = -1;
    //        this.O = -1;
    //    }
    //}
}