using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyWay
{
    public class Road : MonoBehaviour
    {
        //массив по которому определяем соседние участки дороги (сделал так, если вдруг захочу добавить такие участки, которые будут соединять по диагонали)
        [SerializeField] private Vector2[] _nearRoads;
        private Renderer _mainRenderer;
        
        private Vector2Int _position;
        private int _rotation;
        private Road _previousRoad;
        private RoadType _typeRoad = RoadType.Green;

        private void Awake()
        {
            _mainRenderer = GetComponent<Renderer>();
        }

        public void SetTransperent(bool awailable)
        {
            if (awailable)
            {
                SetColor(GetColorByType());
            }
            else
            {
                SetColor(Color.gray);
            }
        }

        public void SetNormalState()
        {
            SetColor(GetColorByType());
            InitPositionData();
        }

        private Color GetColorByType()
        {
            switch (_typeRoad)
            {
                case RoadType.Blue: return Color.blue;
                case RoadType.Red: return Color.red;
            }
            return Color.green;
        }

        private void SetColor(Color color)
        {
            _mainRenderer.material.color = color;
            foreach (var childRenderer in GetComponentsInChildren<Renderer>())
            {
                childRenderer.material.color = color;

            }
        }

        private void InitPositionData()
        {
            _position = new Vector2Int((int)transform.position.x, (int)transform.position.z);
            _rotation = (int)transform.eulerAngles.y;
        }

        //переопределяем рядом стоящие участки дороги
        public List<Vector2Int> GetNearPoint()
        {
            List<Vector2Int> nearPoints = new List<Vector2Int>();
            
            for (int i = 0; i < _nearRoads.Length; i++)
            {
                Vector2Int nearPoint = new Vector2Int(  (int)(_nearRoads[i].x * Mathf.Cos(_rotation* Mathf.Deg2Rad) + _nearRoads[i].y * Mathf.Sin(_rotation* Mathf.Deg2Rad)),
                    (int)(-_nearRoads[i].x * Mathf.Sin(_rotation* Mathf.Deg2Rad) + _nearRoads[i].y * Mathf.Cos(_rotation* Mathf.Deg2Rad))) + _position;

                nearPoints.Add(nearPoint);
            }

            return nearPoints;
        }

        public void SetPreviousRoad(Road previousRoad)
        {
            _previousRoad = previousRoad;
        }

        public Vector2Int GetPosition()
        {
            return _position;
        }

        public Road GetPreviousRoad()
        {
            return _previousRoad;
        }

        public bool CheckPreviousRoad(Road road)
        {
            if (_previousRoad != null)
            {
                return _previousRoad == road && _previousRoad.CheckPreviousRoad(road);
            }

            return false;
        }

        public void SetType(int type)
        {
            switch (type)
            {
                case 0: _typeRoad = RoadType.Blue; break;
                case 1: _typeRoad = RoadType.Red; break;
                case 2: _typeRoad = RoadType.Green; break;
            }
        }

        public RoadType GetType()
        {
            return _typeRoad;
        }
    }

    public enum RoadType
    {
        Red, Blue, Green
    }
}
