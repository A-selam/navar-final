using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using NavAR.Core.Interfaces;

namespace NavAR.Infrastructure
{
    [RequireComponent(typeof(ARCameraManager))]
    public class ZxingQrScanner : MonoBehaviour, IQrScannerService
    {
        private ARCameraManager cameraManager;
        private IBarcodeReader barcodeReader;
        private bool isScanning = false;
        private Action<string> onQrScannedCallback;
        private Texture2D cameraImageTexture;

        [Header("Editor Testing")]
        [SerializeField] private bool simulateScanInEditor = false;
        [SerializeField] private string editorTestPayload = "Block-H-Floor-0-1";
        
        private float scanInterval = 0.25f;
        private float scanTimer = 0f;
        private int cpuImageAcquireFailCount = 0;

        void Awake()
        {
            cameraManager = GetComponent<ARCameraManager>();
        }

        void Start()
        {
            barcodeReader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions { TryHarder = true, TryInverted = true }
            };
        }

        void OnEnable()
        {
            if (cameraManager != null)
            {
                cameraManager.frameReceived += OnCameraFrameReceived;
            }
        }

        void OnDisable()
        {
            if (cameraManager != null)
            {
                cameraManager.frameReceived -= OnCameraFrameReceived;
            }
        }

        public void StartScanning(Action<string> onQrCodeScanned)
        {
            onQrScannedCallback = onQrCodeScanned;
            isScanning = true;
            scanTimer = scanInterval;
            Debug.Log("[ZxingQrScanner] Camera scanning activated.");

            if (Application.isEditor && simulateScanInEditor)
            {
                Debug.Log("[ZxingQrScanner] Editor mode detected; simulating QR payload for testing.");
                isScanning = false;
                onQrScannedCallback?.Invoke(editorTestPayload);
            }
        }

        public void StopScanning()
        {
            isScanning = false;
            onQrScannedCallback = null;
            Debug.Log("[ZxingQrScanner] Camera scanning deactivated.");
        }

        private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
        {
            if (!isScanning || cameraManager == null) return;

            scanTimer += Time.deltaTime;
            if (scanTimer < scanInterval) return;
            scanTimer = 0f;

            if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                cpuImageAcquireFailCount = 0;
                DecodeImage(image);
            }
            else
            {
                cpuImageAcquireFailCount++;
                if (cpuImageAcquireFailCount % 20 == 0)
                {
                    Debug.LogWarning("[ZxingQrScanner] Unable to acquire CPU camera image. Check AR camera permission/device support for CPU image access.");
                }
            }
        }

        private void DecodeImage(XRCpuImage image)
        {
            try
            {
                var conversionParams = new XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(0, 0, image.width, image.height),
                    outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
                    outputFormat = TextureFormat.RGBA32,
                    transformation = XRCpuImage.Transformation.None
                };

                int size = image.GetConvertedDataSize(conversionParams);
                var buffer = new NativeArray<byte>(size, Allocator.Temp);
                image.Convert(conversionParams, buffer);

                var width = conversionParams.outputDimensions.x;
                var height = conversionParams.outputDimensions.y;
                EnsureTexture(width, height, conversionParams.outputFormat);
                cameraImageTexture.LoadRawTextureData(buffer);
                cameraImageTexture.Apply(false);
                buffer.Dispose();

                var result = barcodeReader.Decode(cameraImageTexture.GetPixels32(), width, height);

                if (result != null && !string.IsNullOrEmpty(result.Text))
                {
                    var callback = onQrScannedCallback;
                    var qrText = result.Text.Trim();
                    StopScanning(); 
                    Debug.Log($"[ZxingQrScanner] SUCCESS! Scanned QR Code: {qrText}");
                    callback?.Invoke(qrText);
                }
            }
            finally
            {
                image.Dispose(); 
            }
        }

        private void EnsureTexture(int width, int height, TextureFormat format)
        {
            if (cameraImageTexture != null &&
                cameraImageTexture.width == width &&
                cameraImageTexture.height == height &&
                cameraImageTexture.format == format)
            {
                return;
            }

            if (cameraImageTexture != null)
            {
                Destroy(cameraImageTexture);
            }

            cameraImageTexture = new Texture2D(width, height, format, false);
        }

        void OnDestroy()
        {
            if (cameraImageTexture != null)
            {
                Destroy(cameraImageTexture);
                cameraImageTexture = null;
            }
        }
    }
}