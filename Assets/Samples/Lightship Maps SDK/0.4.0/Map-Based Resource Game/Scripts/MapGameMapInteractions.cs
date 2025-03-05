// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using Niantic.Lightship.Maps.Core.Coordinates;
using Niantic.Lightship.Maps.MapLayers.Components;
using UnityEngine;

namespace Niantic.Lightship.Maps.Samples.GameSample
{
    internal class MapGameMapInteractions : MonoBehaviour
    {
        [SerializeField]
        private Camera _mapCamera;

        [SerializeField]
        private LightshipMapView _lightshipMapView;

        [SerializeField]
        private FloatingText.FloatingText _floatingTextPrefab;

        [SerializeField]
        private LayerGameObjectPlacement _sawmillSpawner;

        [SerializeField]
        private LayerGameObjectPlacement _stoneMasonSpawner;

        [SerializeField]
        private LayerGameObjectPlacement _strongholdSpawner;

        private MapGameState.StructureType _placingStructureType;
        private bool _placingStructure;

        public void StartPlacingStructure(MapGameState.StructureType structureType)
        {
            _placingStructureType = structureType;
            _placingStructure = true;
        }

        private void Update()
        {
            var touchPosition = Vector3.zero;
            bool touchDetected = false;

            if (Input.touchCount == 1)
            {
                if (Input.touches[0].phase == TouchPhase.Ended)
                {
                    touchPosition = Input.touches[0].position;
                    touchDetected = true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                touchPosition = Input.mousePosition;
                touchDetected = true;
            }

            if (touchDetected)
            {
                if (_placingStructure)
                {
                    PlaceStructure(touchPosition);
                }
                else
                {
                    CheckForInteractableTouch(touchPosition);
                }
            }
        }

        private LatLng ScreenPointToLatLong(Vector3 screenPosition)
        {
            var clickRay = _mapCamera.ScreenPointToRay(screenPosition);
            var pointOnMap = clickRay.origin + clickRay.direction * (-clickRay.origin.y / clickRay.direction.y);
            LatLng latLng = _lightshipMapView.SceneToLatLng(pointOnMap);
            return latLng;
        }

        private void PlaceStructure(Vector3 touchPosition)
        {
            var structureLatLng = ScreenPointToLatLong(touchPosition);
            var cameraForward = _mapCamera.transform.forward;
            var forward = new Vector3(cameraForward.x, 0f, cameraForward.z).normalized;
            var rotation = Quaternion.LookRotation(forward);

            switch (_placingStructureType)
            {
                case MapGameState.StructureType.Sawmill:
                    _sawmillSpawner.PlaceInstance(structureLatLng, rotation);
                    break;

                case MapGameState.StructureType.StoneMason:
                    _stoneMasonSpawner.PlaceInstance(structureLatLng, rotation);
                    MapGameState.Instance.EnableResourceProduction(MapGameState.ResourceType.Stone, true);
                    break;

                case MapGameState.StructureType.Stronghold:
                    _strongholdSpawner.PlaceInstance(structureLatLng, rotation);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(_placingStructureType));
            }

            MapGameState.Instance.StructureBuilt(structureLatLng, _placingStructureType);
            
            _placingStructure = false;
        }

        private void CheckForInteractableTouch(Vector3 touchPosition)
        {
            var touchRay = _mapCamera.ScreenPointToRay(touchPosition);

            if (!Physics.Raycast(touchRay, out var hitInfo))
            {
                return;
            }

            var hitResourceItem = hitInfo.collider.GetComponent<MapGameResourceFeature>();
            if (hitResourceItem == null)
            {
                return;
            }

            if (!hitResourceItem.ResourcesAvailable)
            {
                return;
            }

            int amount = hitResourceItem.GainResources();
            MapGameState.Instance.AddResource(hitResourceItem.ResourceType, amount);
            var floatingTextPosition = hitInfo.point + Vector3.up * 20.0f;
            var forward = floatingTextPosition - _mapCamera.transform.position;
            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            var floatText = Instantiate(_floatingTextPrefab, floatingTextPosition, rotation);
            floatText.SetText($"+{amount} {hitResourceItem.ResourceType}");
        }
    }
}