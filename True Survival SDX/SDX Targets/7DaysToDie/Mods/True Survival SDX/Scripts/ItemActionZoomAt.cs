using System;
using SDX.Payload;
using System.Runtime.InteropServices;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections;

public class ItemActionZoomAt : ItemActionZoom
{
    private bool doDebug = false;
    private void DoDebug(string str)
    {
        if (doDebug) Debug.Log("ATTACHMENTS - > " + str);
    }
    private float myQ = 0;
    private string lastZoomOverlay = "";
    private Texture2D ZoomOverlayMod = null;
    private int ZoomOutMod = 0;
    private int ZoomInMod = 0;

    public override void ReadFrom(DynamicProperties _props)
    {
        base.ReadFrom(_props);
        myQ = ZoomOut;
        ZoomOutMod = ZoomOut;
        ZoomInMod = ZoomIn;
        ZoomOverlayMod = this.ZoomOverlay;
        lastZoomOverlay = "";
    }

    public override void OnScreenOverlay(ItemActionData _actionData)
    {
        // get custom zoomoverlay               
        try
        {
            string newZoomOverlay = "";
            int newZoomOut = 0;       
            try
            {                
                #region Get custom properties;
                if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments != null)
                {
                    if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length > 1)
                    {
                        // search for reloading time modifier
                        for (int i = 1;
                            i <=
                            (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length - 1);
                            i++)
                        {
                            if (
                                !_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i].IsEmpty
                                    ())
                            {
                                if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i] !=
                                    ItemValue.None)
                                {
                                    // an attachment exists, check if it has reloading time propertie
                                    ItemClass attach =
                                        ItemClass.GetForId(
                                            _actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments
                                                [i]
                                                .type);
                                    try
                                    {
                                        if (attach.Properties.Classes.ContainsKey("AttachAction1"))
                                        {
                                            DynamicProperties dynamicProperties =
                                                attach.Properties.Classes["AttachAction1"];
                                            if (dynamicProperties.Contains("Zoom_overlay"))
                                            {
                                                newZoomOverlay = dynamicProperties.Values["Zoom_overlay"];
                                                if (lastZoomOverlay != newZoomOverlay)
                                                {
                                                    lastZoomOverlay = newZoomOverlay;
                                                    //DisplayChatAreaText(string.Format("Setting sound to Sound_start={0}", dynamicProperties.Values["Sound_start"]));
                                                    ZoomOverlayMod =
                                                        ResourceWrapper.Load1P(newZoomOverlay)
                                                            as
                                                            Texture2D;
                                                }
                                            }
                                            if (dynamicProperties.Contains("Zoom_max_out"))
                                            {
                                                newZoomOut = Convert.ToInt32(dynamicProperties.Values["Zoom_max_out"]);
                                                ZoomOutMod = newZoomOut;
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
            if (newZoomOverlay == "")
            {
                ZoomOverlayMod = this.ZoomOverlay;
                lastZoomOverlay = "";
            }
            if (newZoomOut == 0) ZoomOutMod = this.ZoomOut;
            if (!((Object)ZoomOverlayMod != (Object)null) || (!_actionData.invData.holdingEntity.AimingGun || !UnityEngine.Event.current.type.Equals((object)UnityEngine.EventType.Repaint)))
                return;
            if ((Object)_actionData.invData.model != (Object)null && ((EntityPlayerLocal)_actionData.invData.holdingEntity).gunCameraTransform.GetComponent<Camera>().enabled)
            {
                ((EntityPlayerLocal)_actionData.invData.holdingEntity).gunCameraTransform.GetComponent<Camera>().enabled = false;
                if (_actionData.invData.holdingEntity.GetModelLayer() != 10)
                {
                    //ei.P = ei.invData.holdingEntity.GetModelLayer();
                    _actionData.invData.holdingEntity.SetModelLayer(10);
                }
            }
            float num1 = (float) ZoomOverlayMod.width;
            float height = (float) Screen.height*0.95f;
            float width = num1*(height/(float) ZoomOverlayMod.height);
            int num2 = (int) (((double) Screen.width - (double) width)/2.0);
            int num3 = (int) (((double) Screen.height - (double) height)/2.0);
            GUIUtils.DrawFilledRect(new Rect(0.0f, 0.0f, (float) Screen.width, (float) num3), Color.black, false,
                Color.black);
            GUIUtils.DrawFilledRect(new Rect(0.0f, 0.0f, (float) num2, (float) Screen.height), Color.black, false,
                Color.black);
            GUIUtils.DrawFilledRect(new Rect((float) num2 + width, 0.0f, (float) Screen.width, (float) num3 + height),
                Color.black, false, Color.black);
            GUIUtils.DrawFilledRect(new Rect(0.0f, (float) num3 + height, (float) Screen.width, (float) Screen.height),
                Color.black, false, Color.black);
            Graphics.DrawTexture(new Rect((float) num2, (float) num3, width, height), (Texture) ZoomOverlayMod);
        }
        catch (Exception ex)
        {
            DoDebug(string.Format("ERROR OnScreenOverlay -> {0}", ex.Message));
        }
    }

    public override bool ConsumeScrollWheel(ItemActionData _actionData, float _scrollWheelInput)
    {
        if (!_actionData.invData.holdingEntity.AimingGun)
            return false;
        //ItemActionZoomAt.EI ei = (ItemActionZoomAt.EI)_actionData;
        //if (!ei.S)
        {
            int newZoomIn = 0;
            int newZoomOut = 0;
            try
            {
                #region Get custom properties;
                if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments != null)
                {
                    if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length > 1)
                    {
                        // search for reloading time modifier
                        for (int i = 1;
                            i <=
                            (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length - 1);
                            i++)
                        {
                            if (
                                !_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i].IsEmpty
                                    ())
                            {
                                if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i] !=
                                    ItemValue.None)
                                {
                                    // an attachment exists, check if it has reloading time propertie
                                    ItemClass attach =
                                        ItemClass.GetForId(
                                            _actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments
                                                [i]
                                                .type);
                                    try
                                    {
                                        if (attach.Properties.Classes.ContainsKey("AttachAction1"))
                                        {
                                            DynamicProperties dynamicProperties =
                                                attach.Properties.Classes["AttachAction1"];
                                            if (dynamicProperties.Contains("Zoom_max_in"))
                                            {
                                                newZoomIn = Convert.ToInt32(dynamicProperties.Values["Zoom_max_in"]);
                                                ZoomInMod = newZoomIn;
                                            }
                                            if (dynamicProperties.Contains("Zoom_max_out"))
                                            {
                                                newZoomOut = Convert.ToInt32(dynamicProperties.Values["Zoom_max_out"]);
                                                if (newZoomOut != ZoomOutMod)
                                                {
                                                    ZoomOutMod = newZoomOut;
                                                    myQ = ZoomOutMod;
                                                }
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
            if (newZoomIn == 0) ZoomInMod = this.ZoomIn;
            if (newZoomOut == 0) ZoomOutMod = this.ZoomOut;
            myQ = Utils.FastClamp(myQ + _scrollWheelInput * -25f, (float)ZoomInMod, (float)ZoomOutMod);
           //DoDebug(string.Format("ZoomIn={0}, ZoomOut={1}, myQ={2}", ZoomInMod, ZoomOutMod, myQ));
            ((EntityPlayerLocal)_actionData.invData.holdingEntity).cameraTransform.GetComponent<Camera>().fieldOfView = (float)(int)myQ;
        }
        return true;
    }

    public override bool IsHUDDisabled(ItemActionData _data)
    {
        string newZoomOverlay = "";
        try
        {
            #region Get custom properties;
            if (_data.invData.holdingEntity.inventory.holdingItemItemValue.Attachments != null)
            {
                if (_data.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length > 1)
                {
                    // search for reloading time modifier
                    for (int i = 1;
                        i <=
                        (_data.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length - 1);
                        i++)
                    {
                        if (
                            !_data.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i].IsEmpty
                                ())
                        {
                            if (_data.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i] !=
                                ItemValue.None)
                            {
                                // an attachment exists, check if it has reloading time propertie
                                ItemClass attach =
                                    ItemClass.GetForId(
                                        _data.invData.holdingEntity.inventory.holdingItemItemValue.Attachments
                                            [i]
                                            .type);
                                try
                                {
                                    if (attach.Properties.Classes.ContainsKey("AttachAction1"))
                                    {
                                        DynamicProperties dynamicProperties =
                                            attach.Properties.Classes["AttachAction1"];
                                        if (dynamicProperties.Contains("Zoom_overlay"))
                                        {
                                            newZoomOverlay = dynamicProperties.Values["Zoom_overlay"];
                                            if (lastZoomOverlay != newZoomOverlay)
                                            {
                                                lastZoomOverlay = newZoomOverlay;
                                                //DisplayChatAreaText(string.Format("Setting sound to Sound_start={0}", dynamicProperties.Values["Sound_start"]));
                                                ZoomOverlayMod =
                                                    ResourceWrapper.Load1P(newZoomOverlay)
                                                        as
                                                        Texture2D;
                                            }
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
        if (newZoomOverlay == "")
        {
            ZoomOverlayMod = this.ZoomOverlay;
            lastZoomOverlay = "";
        }
        if ((Object)ZoomOverlayMod != (Object)null && !_data.invData.holdingEntity.isEntityRemote && _data.invData.holdingEntity.AimingGun)
            return true;
        return false;
    }

    public override void GetIronSights(ItemActionData _actionData, out float _fov)
    {
        string newZoomOverlay = "";
        int newZoomOut = 0;
        try
        {
            #region Get custom properties;
            if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments != null)
            {
                if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length > 1)
                {
                    // search for reloading time modifier
                    for (int i = 1;
                        i <=
                        (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments.Length - 1);
                        i++)
                    {
                        if (
                            !_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i].IsEmpty
                                ())
                        {
                            if (_actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments[i] !=
                                ItemValue.None)
                            {
                                // an attachment exists, check if it has reloading time propertie
                                ItemClass attach =
                                    ItemClass.GetForId(
                                        _actionData.invData.holdingEntity.inventory.holdingItemItemValue.Attachments
                                            [i]
                                            .type);
                                try
                                {
                                    if (attach.Properties.Classes.ContainsKey("AttachAction1"))
                                    {
                                        DynamicProperties dynamicProperties =
                                            attach.Properties.Classes["AttachAction1"];
                                        if (dynamicProperties.Contains("Zoom_overlay"))
                                        {
                                            newZoomOverlay = dynamicProperties.Values["Zoom_overlay"];
                                            if (lastZoomOverlay != newZoomOverlay)
                                            {
                                                lastZoomOverlay = newZoomOverlay;
                                                //DisplayChatAreaText(string.Format("Setting sound to Sound_start={0}", dynamicProperties.Values["Sound_start"]));
                                                ZoomOverlayMod =
                                                    ResourceWrapper.Load1P(newZoomOverlay)
                                                        as
                                                        Texture2D;
                                            }
                                        }
                                        if (dynamicProperties.Contains("Zoom_max_out"))
                                        {
                                            newZoomOut = Convert.ToInt32(dynamicProperties.Values["Zoom_max_out"]);
                                            ZoomOutMod = newZoomOut;
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
            if (newZoomOverlay == "") ZoomOverlayMod = this.ZoomOverlay;
            if (newZoomOut == 0) ZoomOutMod = this.ZoomOut;
            _fov = !((Object) ZoomOverlayMod == (Object) null) ? 0.0f : (float) ZoomOutMod;
        }
        catch (Exception ex)
        {
            DoDebug(string.Format("ERROR GetIronSights -> {0}, ZOOMOVERLAY={1}", ex.Message, ZoomOverlayMod.ToString()));
            _fov = 0;
        }
    }

    public override void OnHoldingUpdate(ItemActionData _actionData)
    {
        base.OnHoldingUpdate(_actionData);
        if (!_actionData.invData.holdingEntity.AimingGun || !(_actionData.invData.holdingEntity is EntityPlayerLocal))
            return;
        ((EntityPlayerLocal) _actionData.invData.holdingEntity).cameraTransform.GetComponent<Camera>().fieldOfView =
            (float) (int) myQ;
    }
}