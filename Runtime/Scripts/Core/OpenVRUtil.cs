using UnityEngine;
using Valve.VR;
using System;
using System.Collections.Generic;
using static SteamVR_Utils;

internal static class OpenVRUtil
{
    internal static class Sys
    {
        internal static bool IsOpenVRAvailable()
        {
            return OpenVR.System != null;
        }

        internal static void InitOpenVR()
        {
            if (OpenVR.System != null) return;

            var error = EVRInitError.None;
            OpenVR.Init(ref error, EVRApplicationType.VRApplication_Overlay);
            if (error != EVRInitError.None)
            {
                throw new Exception("OpenVRの初期化に失敗しました: " + error);
            }
        }

        internal static void ShutdownOpenVR()
        {
            if (OpenVR.System != null)
            {
                OpenVR.Shutdown();
            }
        }

        internal static SteamVR_Utils.RigidTransform GetControllerTransform(uint controllerIndex)
        {
            //default Transform
            var pos = new Vector3(0f, 0f, 0f);
            var rot = Quaternion.Euler(0, 0, 0);
            var defaultTransform = new SteamVR_Utils.RigidTransform(pos, rot);

            var poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            if (OpenVR.System == null) return defaultTransform;
            OpenVR.System.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0.0f, poses);
            if (!poses[controllerIndex].bPoseIsValid) return defaultTransform;
            else return new SteamVR_Utils.RigidTransform(poses[controllerIndex].mDeviceToAbsoluteTracking);
        }
        
        internal static OverlayAnchor DeviceToAnchor(uint deviceIndex)
        {
            if (deviceIndex == OpenVR.k_unTrackedDeviceIndex_Hmd) return OverlayAnchor.HMD;

            if (deviceIndex == OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand)) return OverlayAnchor.LeftHand;
            if (deviceIndex == OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand)) return OverlayAnchor.RightHand;
            if (deviceIndex == OpenVR.k_unTrackedDeviceIndex_Hmd) return OverlayAnchor.HMD;
            return OverlayAnchor.None;
        }
    }

    internal static class Overlay
    {
        internal static bool IsOverlayAvailable()
        {
            return OpenVR.Overlay != null;
        }

        internal static ulong CreateOverlay(string key, string name)
        {
            var handle = OpenVR.k_ulOverlayHandleInvalid;
            var error = OpenVR.Overlay?.CreateOverlay(key, name, ref handle);
            if (error.GetValueOrDefault(EVROverlayError.None) != EVROverlayError.None)
            {
                throw new Exception("オーバーレイの作成に失敗しました: " + error);
            }

            return handle;
        }

        internal static (ulong, ulong) CreateDashboardOverlay(string key, string name)
        {
            ulong dashboardHandle = 0;
            ulong thumbnailHandle = 0;
            var error = OpenVR.Overlay?.CreateDashboardOverlay(key, name, ref dashboardHandle, ref thumbnailHandle);
            if (error.GetValueOrDefault(EVROverlayError.None) != EVROverlayError.None)
            {
                throw new Exception("ダッシュボードオーバーレイの作成に失敗しました: " + error);
            }

            return (dashboardHandle, thumbnailHandle);
        }

        internal static void DestroyOverlay(ulong handle)
        {
            if (handle != OpenVR.k_ulOverlayHandleInvalid)
            {
                var error = OpenVR.Overlay?.DestroyOverlay(handle);
                if (error.GetValueOrDefault(EVROverlayError.None) != EVROverlayError.None)
                {
                    throw new Exception("オーバーレイの破棄に失敗しました: " + error);
                }
            }
        }

        internal static void SetOverlayFromFile(ulong handle, string path)
        {
            var error = OpenVR.Overlay?.SetOverlayFromFile(handle, path);
            if (error.GetValueOrDefault(EVROverlayError.None) != EVROverlayError.None)
            {
                throw new Exception("画像ファイルの描画に失敗しました: " + error);
            }
        }

        internal static void ShowOverlay(ulong handle)
        {
            var error = OpenVR.Overlay?.ShowOverlay(handle);
            if (error.GetValueOrDefault(EVROverlayError.None) != EVROverlayError.None)
            {
                throw new Exception("オーバーレイの表示に失敗しました: " + error);
            }
        }

        internal static void SetOverlaySize(ulong handle, float size)
        {
            var error = OpenVR.Overlay?.SetOverlayWidthInMeters(handle, size);
            if (error.GetValueOrDefault(EVROverlayError.None) != EVROverlayError.None)
            {
                throw new Exception("オーバーレイのサイズ設定に失敗しました: " + error);
            }
        }

        internal static void SetOverlayTransformRelative(ulong handle, uint deviceIndex, RigidTransform posRot)
        {
            var rigidTransform = new RigidTransform(posRot.pos, posRot.rot);
            var matrix = rigidTransform.ToHmdMatrix34();
            var error = OpenVR.Overlay?.SetOverlayTransformTrackedDeviceRelative(handle, deviceIndex, ref matrix);
            if (error.GetValueOrDefault(EVROverlayError.None) != EVROverlayError.None)
            {
                throw new Exception("オーバーレイの位置設定に失敗しました: " + error);
            }
        }

        internal static void FlipOverlayVertical(ulong handle)
        {
            var bounds = new VRTextureBounds_t
            {
                uMin = 0,
                uMax = 1,
                vMin = 1,
                vMax = 0
            };

            var error = OpenVR.Overlay?.SetOverlayTextureBounds(handle, ref bounds);
            if (error.GetValueOrDefault(EVROverlayError.None) != EVROverlayError.None)
            {
                throw new Exception("テクスチャの反転に失敗しました: " + error);
            }
        }

        internal static void SetOverlayRenderTexture(ulong handle, RenderTexture renderTexture)
        {
            if (!renderTexture.IsCreated()) return;

            var nativeTexturePtr = renderTexture.GetNativeTexturePtr();
            var texture = new Texture_t
            {
                eColorSpace = EColorSpace.Auto,
                eType = ETextureType.DirectX,
                handle = nativeTexturePtr
            };
            var error = OpenVR.Overlay?.SetOverlayTexture(handle, ref texture);
            if (error.GetValueOrDefault(EVROverlayError.None) != EVROverlayError.None)
            {
                throw new Exception("テクスチャの描画に失敗しました: " + error);
            }
        }

        internal static void SetOverlayMouseScale(ulong handle, int x, int y)
        {
            var pvecMouseScale = new HmdVector2_t()
            {
                v0 = x,
                v1 = y
            };
            var error = OpenVR.Overlay?.SetOverlayMouseScale(handle, ref pvecMouseScale);
            if (error.GetValueOrDefault(EVROverlayError.None) != EVROverlayError.None)
            {
                throw new Exception("マウススケールの設定に失敗しました: " + error);
            }
        }

        /// <summary>
        /// コントローラーの位置と角度からオーバーレイの交差点を取得
        /// ピクセル単位で左上を原点とする座標を返す
        /// </summary>
        /// <param name="overlayHandle"></param>
        /// <param name="controllerIndex"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        internal static Vector2 GetOverlayIntersectionForController(ulong overlayHandle, uint controllerIndex, RenderTexture renderTexture, int angle = 45)
        {
            var hitPoint = new Vector2(0f, 0f);
            var controllerTransform = Sys.GetControllerTransform(controllerIndex);
            var direction = controllerTransform.rot * Quaternion.AngleAxis(angle, Vector3.right) * Vector3.forward;
            var overlayParams = new VROverlayIntersectionParams_t
            {
                vSource = new HmdVector3_t
                {
                    v0 = controllerTransform.pos.x,
                    v1 = controllerTransform.pos.y,
                    v2 = -controllerTransform.pos.z
                },
                vDirection = new HmdVector3_t
                {
                    v0 = direction.x,
                    v1 = direction.y,
                    v2 = -direction.z
                },
                eOrigin = ETrackingUniverseOrigin.TrackingUniverseStanding
            };
            VROverlayIntersectionResults_t overlayResults = default;

            var hit = OpenVR.Overlay?.ComputeOverlayIntersection(overlayHandle, ref overlayParams, ref overlayResults);
            if (hit ?? false)
            {
                hitPoint.x = overlayResults.vUVs.v0 * renderTexture.width;
                hitPoint.y = -overlayResults.vUVs.v1 * renderTexture.height;
            }
            return hitPoint;
        }

        /// <summary>
        /// ダッシュボードでのみ使用可
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="rt"></param>
        /// <returns></returns>
        internal static Vector2 GetEventMousePosition(VREvent_t ev, RenderTexture rt)
        {
            return new Vector2(ev.data.mouse.x, rt.height - ev.data.mouse.y);
        }

        /// <summary>
        /// ダッシュボードでのみ使用可
        /// </summary>
        /// <param name="overlayHandle"></param>
        /// <returns></returns>
        internal static IEnumerable<VREvent_t> PollOverlayEvents(ulong overlayHandle)
        {
            var vrEvent = new VREvent_t();
            var uncbVREvent = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VREvent_t));
            while (OpenVR.Overlay != null && OpenVR.Overlay.PollNextOverlayEvent(overlayHandle, ref vrEvent, uncbVREvent))
            {
                yield return vrEvent;
            }
        }

        internal static void SetOverlayInputMouse(ulong overlayHandle, bool on)
        {
            VROverlayInputMethod eInputMethod = on ? VROverlayInputMethod.Mouse : VROverlayInputMethod.None;
            var error = OpenVR.Overlay?.SetOverlayInputMethod(overlayHandle, eInputMethod);
            // Debug.Log($"SetOverlayInputMethod: {eInputMethod}");
            if (error.GetValueOrDefault(EVROverlayError.None) != EVROverlayError.None)
            {
                throw new Exception("オーバーレイの入力方法の設定に失敗しました: " + error);
            }
        }

        internal static void SetOverlayIntersectionMask(ulong overlayHandle, IntersectionMaskRectangle_t rect, Vector2Int wholeSize)
        {

            VROverlayIntersectionMaskPrimitive_t a = new()
            {
                m_nPrimitiveType = EVROverlayIntersectionMaskPrimitiveType.OverlayIntersectionPrimitiveType_Rectangle,
                m_Primitive = new()
                {
                    m_Rectangle = new()
                    {
                        m_flTopLeftX = wholeSize.x - rect.m_flTopLeftX,
                        m_flTopLeftY = wholeSize.y - rect.m_flTopLeftY,
                        m_flWidth = rect.m_flWidth,
                        m_flHeight = rect.m_flHeight
                    }
                }
            };
            var unPrimitiveSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VROverlayIntersectionMaskPrimitive_t));
            var error = OpenVR.Overlay?.SetOverlayIntersectionMask(overlayHandle, ref a, 1, unPrimitiveSize);
            if (error.GetValueOrDefault(EVROverlayError.None) != EVROverlayError.None)
            {
                throw new Exception("交差マスクの設定に失敗しました: " + error);
            }
        }

        internal static void SendVRSmoothScrollEvents(ulong overlayHandle, bool b)
        {
            var error = OpenVR.Overlay?.SetOverlayFlag(overlayHandle, VROverlayFlags.SendVRSmoothScrollEvents, b);
            if (error.GetValueOrDefault(EVROverlayError.None) != EVROverlayError.None)
            {
                throw new Exception("スクロールのフラグの設定に失敗しました: " + error);
            }
        }

        internal static void MakeOverlaysInteractiveIfVisible(ulong overlayHandle, bool b)
        {
            var error = OpenVR.Overlay?.SetOverlayFlag(overlayHandle, VROverlayFlags.MakeOverlaysInteractiveIfVisible, b);
            // Debug.Log($"MakeOverlaysInteractiveIfVisible: {b}");
            if (error.GetValueOrDefault(EVROverlayError.None) != EVROverlayError.None)
            {
                throw new Exception("オーバーレイのフラグの設定に失敗しました: " + error);
            }
        }

        internal static void MultiCursor(ulong overlayHandle, bool b)
        {
            var error = OpenVR.Overlay?.SetOverlayFlag(overlayHandle, VROverlayFlags.MultiCursor, b);
            if (error.GetValueOrDefault(EVROverlayError.None) != EVROverlayError.None)
            {
                throw new Exception("マルチカーソルのフラグの設定に失敗しました: " + error);
            }
        }

        internal static bool IsHoverTargetOverlay(ulong overlayHandle)
        {
            return OpenVR.Overlay?.IsHoverTargetOverlay(overlayHandle) ?? false;
        }
    }

    internal static class Actions
    {
        internal static bool IsInputAvailable()
        {
            return OpenVR.Input != null;
        }

        internal static void SetActionManifestPath(string actionManifestPath)
        {
            var error = OpenVR.Input?.SetActionManifestPath(actionManifestPath);
            if (error.GetValueOrDefault(EVRInputError.None) != EVRInputError.None)
            {
                throw new Exception("Action Manifest パスの指定に失敗しました: " + error);
            }
        }

        internal static ulong GetActionSetHandle(string actionSetName)
        {
            ulong actionSetHandle = 0;
            var error = OpenVR.Input?.GetActionSetHandle(actionSetName, ref actionSetHandle);
            if (error.GetValueOrDefault(EVRInputError.None) != EVRInputError.None)
            {
                throw new Exception($"アクションセット {actionSetName} の取得に失敗しました: " + error);
            }

            return actionSetHandle;
        }

        internal static ulong GetActionHandle(string actionName)
        {
            ulong actionHandle = 0;
            var error = OpenVR.Input?.GetActionHandle(actionName, ref actionHandle);
            if (error.GetValueOrDefault(EVRInputError.None) != EVRInputError.None)
            {
                throw new Exception($"アクション {actionName} の取得に失敗しました: " + error);
            }

            return actionHandle;
        }

        internal static void UpdateActionState(ulong actionSetHandle)
        {
            // 更新したいアクションセットのリスト
            var actionSetList = new VRActiveActionSet_t[]
            {
                    new()
                    {
                        ulActionSet = actionSetHandle,
                        ulRestrictedToDevice = OpenVR.k_ulInvalidInputValueHandle,
                    }
            };

            var activeActionSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRActiveActionSet_t));
            var error = OpenVR.Input?.UpdateActionState(actionSetList, activeActionSize);
            if (error.GetValueOrDefault(EVRInputError.None) != EVRInputError.None)
            {
                throw new Exception("アクションの状態の更新に失敗しました: " + error);
            }
        }

        internal static InputDigitalActionData_t GetDigitalActionData(ulong actionHandle)
        {
            var result = new InputDigitalActionData_t();
            var digitalActionSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(InputDigitalActionData_t));
            var error = OpenVR.Input?.GetDigitalActionData(actionHandle, ref result, digitalActionSize, OpenVR.k_ulInvalidInputValueHandle);
            if (error.GetValueOrDefault(EVRInputError.None) != EVRInputError.None)
            {
                throw new Exception("Digital アクションのデータ取得に失敗しました: " + error);
            }

            return result;
        }

        internal static InputAnalogActionData_t GetAnalogActionData(ulong actionHandle)
        {
            var result = new InputAnalogActionData_t();
            var digitalActionSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(InputAnalogActionData_t));
            var error = OpenVR.Input?.GetAnalogActionData(actionHandle, ref result, digitalActionSize, OpenVR.k_ulInvalidInputValueHandle);
            if (error.GetValueOrDefault(EVRInputError.None) != EVRInputError.None)
            {
                throw new Exception("Analog アクションのデータ取得に失敗しました: " + error);
            }

            return result;
        }

        internal static InputOriginInfo_t GetOriginTrackedDeviceInfo(ulong activeOrigin)
        {
            InputOriginInfo_t inputOrigin = new();
            var unOriginInfoSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(InputOriginInfo_t));
            var error = OpenVR.Input?.GetOriginTrackedDeviceInfo(activeOrigin, ref inputOrigin, unOriginInfoSize);
            if (error.GetValueOrDefault(EVRInputError.None) != EVRInputError.None)
            {
                throw new Exception("GetOriginTrackedDeviceInfo failed: " + error);
            }

            return inputOrigin;
        }
    }
}