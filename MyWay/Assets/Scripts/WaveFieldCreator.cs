using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace MyWay
{
    /// <summary>
    /// Build game field
    /// </summary>
    public class WaveFieldCreator : MonoBehaviour
    {
        [SerializeField] private GameObject _fieldPrefab;
        [SerializeField] private GameObject _cube_1;                        //blue cube
        [SerializeField] private GameObject _cube_2;                        //red cube
        [SerializeField, Range(1,4), Tooltip("кол-во клеток в ширину от центральной клетки")]private int _halfWidth = 1;         //Ширина карты (x)
        [SerializeField, Range(1,4), Tooltip("кол-во клеток в длину от центральной клетки")]private int _halfLength = 1;        //Длина карты (z)

        [SerializeField] private Road[] _roadsElements;

        [SerializeField] private GameObject _menuFone;
        [SerializeField] private GameObject _pauseFone;
        [SerializeField] private TextMeshProUGUI _currentTextTime;
        [SerializeField] private TextMeshProUGUI _bestTextTime;

        private Dictionary<Vector2, Road> _gridRoad = new Dictionary<Vector2, Road>();
        private Road _flyingRoad;
        private Camera _mainCamera;

        private List<Vector2Int> _pathBlue = new List<Vector2Int>();
        private List<Vector2Int> _pathRed = new List<Vector2Int>();
        
        private bool _cubeIsMoveBlue = false;
        private bool _cubeIsMoveRed = false;
        private int _pathPointBlue = 0;
        private int _pathPointRed = 0;

        private static Random _random = new Random();
        private float _timer = 0;
        private float? _bestTime;
        private bool _timerOn = true;

        private void Awake()
        {
            _mainCamera = Camera.main;
            
            CreateGameField();
            PlacementStartElement();
            Menu();
        }

        private void CreateGameField()
        {
            for (int i = -_halfWidth; i <= _halfWidth; i++)
            {
                for (int j = -_halfLength; j <= _halfLength; j++)
                {
                    Instantiate(_fieldPrefab, new Vector3(i, 0, j), new Quaternion(), transform);
                }
            }
        }

        private void PlacementStartElement()
        {
            Instantiate(_fieldPrefab, new Vector3(0, 0, -_halfLength - 1), new Quaternion(), transform);
            Instantiate(_roadsElements[1], new Vector3(0, 0.1f, -_halfLength - 1), new Quaternion());
            _cube_1 = Instantiate(_cube_1, new Vector3(0, 0.25f, -_halfLength - 0.8f), new Quaternion());
            _cube_2 = Instantiate(_cube_2, new Vector3(0, 0.25f, -_halfLength - 1.2f), new Quaternion());
            Instantiate(_fieldPrefab, new Vector3(0, 0, _halfLength + 1), new Quaternion(), transform);
            Instantiate(_roadsElements[1], new Vector3(0, 0.1f, _halfLength + 1), new Quaternion());
        }

        private void RestartCube()
        {
            _cube_1.transform.position = new Vector3(0, 0.25f, -_halfLength - 0.8f);
            _cube_2.transform.position = new Vector3(0, 0.25f, -_halfLength - 1.2f);
        }

        public void CreateRandomRoad()
        {
            if (_roadsElements.Length > 0)
            {
                StartPlacingRoad(_roadsElements[_random.Next(0,_roadsElements.Length)]);
            }
        }

        public void StartPlacingRoad(Road roadPrefab)
        {
            if (_flyingRoad != null)
            {
                Destroy(_flyingRoad.gameObject);
            }
            _flyingRoad = Instantiate(roadPrefab);
            _flyingRoad.SetType(_random.Next(0,sizeof(RoadType)-1));
        }

        private void Update()
        {
            if (_timerOn)
            {
                _timer += Time.deltaTime;
            }

            _currentTextTime.text = Math.Round(_timer).ToString();
            
            if (_flyingRoad != null)
            {
                var groundPlane = new Plane(Vector3.up, new Vector3(0, 0.1f, 0));
                var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

                if (groundPlane.Raycast(ray, out float position))
                {
                    Vector3 worldPosition = ray.GetPoint(position);

                    int x = Mathf.RoundToInt(worldPosition.x);
                    int z = Mathf.RoundToInt(worldPosition.z);

                    //location check
                    bool available = !(x < -_halfWidth || x > _halfWidth || z < -_halfLength || z > _halfLength)
                                        && !IsPlaceTaken(x,z);

                    PalacmentFlyingRoad(available, x, z);
                }
            }
        }

        private void PalacmentFlyingRoad(bool available, int x, int z)
        {
            _flyingRoad.transform.position = new Vector3(x,0.1f, z);
            _flyingRoad.SetTransperent(available);

            if (Input.mouseScrollDelta.y > 0)
            {
                RotateFlyingRoad(90);
            }

            if (Input.mouseScrollDelta.y < 0)
            {
                RotateFlyingRoad(-90);
            }

            if (available && Input.GetMouseButtonDown(0) && _timerOn)
            {
                PlaceFlyingRoad(x, z);
            }
        }

        private bool IsPlaceTaken(int placeX, int placeZ)
        {
            if (_gridRoad.ContainsKey(new Vector2(placeX, placeZ))) return true;

            return false;
        }

        private void PlaceFlyingRoad(int placeX, int placeZ)
        {
            _gridRoad.Add(new Vector2(placeX, placeZ), _flyingRoad);
            
            _flyingRoad.SetNormalState();
            _flyingRoad = null;
        }

        private void RotateFlyingRoad(float angle)
        {
            _flyingRoad.transform.Rotate(Vector3.up,  angle);
        }

        public void DestroyAllRoad()
        {
            foreach (var road in _gridRoad)
            {
                Destroy(road.Value.gameObject);
            }
            _gridRoad.Clear();
            _pathBlue.Clear();
            _pathRed.Clear();
            _pathPointBlue = 0;
            _pathPointRed = 0;
        }

        public void SearchPath()
        {
            _timerOn = false;
            //проверим, есть ли смысл искать путь
            if (!_gridRoad.ContainsKey(new Vector2(0, -_halfLength))) return ;
            if (!_gridRoad[new Vector2(0, -_halfLength)].GetNearPoint()
                .Contains(new Vector2Int(0, -_halfLength - 1)))
                return;
            
            if (!_gridRoad.ContainsKey(new Vector2(0, _halfLength))) return ;
            if (!_gridRoad[new Vector2(0, _halfLength)].GetNearPoint()
                .Contains(new Vector2Int(0, _halfLength + 1)))
                return;

            Road lastRoadForBlue = CollectPointPath(_gridRoad[new Vector2(0, -_halfLength)], RoadType.Blue);
            if (lastRoadForBlue != null)
            {
                AddPointToPath(lastRoadForBlue, _pathBlue);
                _pathBlue.Reverse();
                _pathBlue.Add(new Vector2Int(0, _halfLength + 1));
                _cubeIsMoveBlue = true;
                StartCoroutine(MoveBlueCubeCoroutine());
            }

            Road lastRoadForRed = CollectPointPath(_gridRoad[new Vector2(0, -_halfLength)], RoadType.Red);
            if (lastRoadForRed != null)
            {
                AddPointToPath(lastRoadForRed, _pathRed);
                _pathRed.Reverse();
                _pathRed.Add(new Vector2Int(0, _halfLength + 1));
                _cubeIsMoveRed = true;
                StartCoroutine(MoveRedCubeCoroutine());
            }

            if (lastRoadForBlue != null && lastRoadForRed != null)
            {
                if (_bestTime.HasValue)
                {
                    _bestTime = _bestTime < _timer ? _bestTime : _timer;
                }
                else
                {
                    _bestTime = _timer;
                }
            }
        }

        private IEnumerator MoveBlueCubeCoroutine()
        {
            while (_cubeIsMoveBlue)
            {
                MouveCube(ref _cube_1, ref _pathBlue, ref _pathPointBlue, ref _cubeIsMoveBlue);
                yield return null;
            }
        }
        
        private IEnumerator MoveRedCubeCoroutine()
        {
            while (_cubeIsMoveRed)
            {
                MouveCube(ref _cube_2,ref _pathRed, ref _pathPointRed, ref _cubeIsMoveRed);
                yield return null;
            }
        }

        private void MouveCube(ref GameObject cube, ref List<Vector2Int> path, ref int pathPoint, ref bool isMove)
        {
            Vector2 currentPosition = new Vector2 (cube.transform.position.x, cube.transform.position.z);
            float delta = Vector2.Distance(currentPosition , path[pathPoint]);
            
            if (delta < 0.1f)
                pathPoint++;
            
            if (pathPoint >= path.Count)
            {
                isMove = false;
                return;
            }
            
            Vector3 modFrameMove = (new Vector3(path[pathPoint].x, cube.transform.position.y, path[pathPoint].y) - cube.transform.position).normalized;
            cube.transform.position += cube.transform.TransformDirection(modFrameMove) * Time.deltaTime;
        }

        private void AddPointToPath(Road road, List<Vector2Int> path)
        {
            if (road == null)
            {
                return;
            }
            Vector2Int position = road.GetPosition();
            path.Add(position);
            AddPointToPath(road.GetPreviousRoad(), path);
            road.SetPreviousRoad(null); //зануляем предыдущий участок, для следующих расчётов
        }

        private Road CollectPointPath(Road road, RoadType roadType)
        {
            //пробегаемся по всем рядом стоящим точкам, на которые возможен переход
            foreach (var nearPos in road.GetNearPoint())
            {
                //если рядом стоящая точка это конечная, то мы нашли последний участок
                if (nearPos == new Vector2Int(0, _halfLength + 1))
                {
                    return road;
                }
                //если в списке, по указаной позиции, есть дорога, то проверяем её
                if (_gridRoad.ContainsKey(nearPos))
                {
                    Road nearRoad = _gridRoad[nearPos];
                    bool needType = nearRoad.GetType() == roadType || nearRoad.GetType() == RoadType.Green; //проверка что данный участок нужного цвета
                    bool needPrevRoad = nearRoad.GetNearPoint().Contains(road.GetPosition());    //проверяем что со следующей точки можно вернуться назад
                    
                    if (road.GetPreviousRoad() != null)
                    {
                        needPrevRoad = needPrevRoad && road.GetPreviousRoad() != nearRoad;
                    }
                    
                    if ( needType && needPrevRoad)
                    {
                        //если где то в предыдущем участке, есть данный участок, то мы входим в кольцо, пропускаем
                        if (!nearRoad.CheckPreviousRoad(road))
                        {
                            nearRoad.SetPreviousRoad(road);
                            Road lastRoad = CollectPointPath(nearRoad, roadType);
                            if (lastRoad == null) continue;
                            return lastRoad;
                        }
                    }
                }
            }

            return null;
        }

        public void SetFieldSize(string size)
        {
             switch (size)
            {
                case "Maximum": _halfLength = _halfWidth = 4; break;
                case "Midle": _halfLength = _halfWidth = 3; break;
                case "Minimum": _halfLength = _halfWidth = 1; break;
            }
        }

        public void Pause()
        {
            _timerOn = false;
            _pauseFone.SetActive(true);
        }

        public void Continue()
        {
            _timerOn = true;
            _pauseFone.SetActive(false);
        }

        public void Menu()
        {
            if (_bestTime.HasValue)
            {
                _bestTextTime.text = Math.Round(_bestTime.Value, 3).ToString();
            }
            _timerOn = false;
            _menuFone.SetActive(true);
        }

        public void StartGame()
        {
            _timer = 0;
            DestroyAllRoad();
            RestartCube();
            _timerOn = transform;
            _menuFone.SetActive(false);
        }
    }
}
