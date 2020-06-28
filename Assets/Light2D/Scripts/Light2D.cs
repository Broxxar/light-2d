using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.SceneManagement;

// Empty class definition for Sorting Layer Attribute
public class SortingLayerAttribute : PropertyAttribute { }

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Light2D : MonoBehaviour
{
	private struct PointAngle
	{
		public Vector2 point;
		public float angle;
	}

	private static Manager _managerBack;

	private static Manager _manager
	{
		get
		{
			if (_managerBack == null)
				_managerBack = new Manager();

			return _managerBack;
		}
	}

	private class Manager
	{
		public List<Light2D> Lights;

		public Manager()
		{
			Lights = new List<Light2D>();
		}

		public void RegisterLight(Light2D light)
		{
			Lights.Add(light);
		}

		public void DeregisterLight(Light2D light)
		{
			if (light != null)
				Lights.Remove(light);
		}

		public void RegisterCollider(Collider2D col)
		{
			for (int i = 0; i < Lights.Count; i++)
			{
				if (Lights[i]._colliders.Contains(col))
					continue;

				Lights[i].RegisterColliderInternal(col);
			}
		}
	}

	/// <summary>
	/// Returns all registed Light2D objects in the scene.
	/// </summary>
	public static List<Light2D> All
	{
		get
		{
			return _manager.Lights;
		}
	}

	public float Range = 5;
	public Color LightColor = Color.white;
	[Range(0, 10)]
	public float Intensity = 1;
	[Range(0, 360)]
	public float Angle = 360;
	public LayerMask ShadowMask = -1;
	[SortingLayer]
	public int SortingLayer;
	public int OrderInLayer;
	public int MaxVertCount = 1000;

	List<Collider2D> _colliders = new List<Collider2D>();
	List<Vector2[]> _pointSets = new List<Vector2[]>();
	List<Vector2> _edgePoints = new List<Vector2>();
	Vector2[] _pointBuffer = new Vector2[64];
	RaycastHit2D[] _edgeHitBuffer = new RaycastHit2D[16];
	List<PointAngle> _raycastHits = new List<PointAngle>();

	MeshFilter _meshFilter;
	MeshRenderer _meshRenderer;
	Vector3[] _vertBuffer;
	Vector2[] _uvBuffer;
	Color[] _colorBuffer;
	int[] _triBuffer;

	private float _maxAngle = 0;
	private int _segments = 10;
	private int _prevSortLayer;
	private int _prevSortOrder;
	private bool _sortDirty = true;

	public void Refresh()
	{
		// Note: LINQ and Find both cause a bit of allocation to be GC'd, Refreshing should only be done when major changes occur.
		// eg. a new level is loaded and this light presists via DontDestroyOnLoad()
		_colliders = FindObjectsOfType<Collider2D>().ToList();
		_pointSets = _colliders.Select(col => GetPoints(col)).ToList();

		_vertBuffer = new Vector3[MaxVertCount];
		_uvBuffer = new Vector2[MaxVertCount];
		_colorBuffer = new Color[MaxVertCount];
		_triBuffer = new int[MaxVertCount * 3];
	}

	/// <summary>
	/// The public method through which a new Collider created a runtime should be registered with Light2D.
	/// This will notify all existing Lights of the colliders existance, from then on it will be treated as any other
	/// object that had existed previously (lights cast based on Layer).
	/// </summary>
	public static void RegisterCollider(Collider2D col)
	{
		_manager.RegisterCollider(col);
	}

	/// <summary>
	/// A GameObject variant of the above method, purely for convience.
	/// </summary>
	public static void RegisterCollider(GameObject go)
	{
		RegisterCollider(go.GetComponent<Collider2D>());
	}

	/// <summary>
	/// Initializes the mesh and material for the light.
	/// </summary>
	private void Init()
	{
		_meshFilter = GetComponent<MeshFilter>();
		_meshRenderer = GetComponent<MeshRenderer>();

		if (_meshFilter.sharedMesh != null)
			DestroyImmediate(_meshFilter.sharedMesh);

		if (_meshRenderer.sharedMaterial == null)
			_meshRenderer.sharedMaterial = Resources.Load<Material>("Light2D-Additive");

		_manager.RegisterLight(this);
		SceneManager.sceneLoaded += OnSceneLoaded;
    }

	void Awake()
	{
		Init();
		Refresh();
	}

	void OnValidate()
	{
		Refresh();
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		Refresh();
	}

	void Update()
	{
		#region Editor Only
#if UNITY_EDITOR
		// Constantly refreshes in the editor while not playing since we don't care too much about GC hiccups here
		if (!UnityEditor.EditorApplication.isPlaying)
			Refresh();
#endif
		#endregion
		LockRotation();
		CastRays();
		DrawMesh();
		UpdateSorting();
	}

	private void UpdateSorting()
	{
		_sortDirty = _prevSortLayer != SortingLayer || _prevSortOrder != OrderInLayer;

		if (_sortDirty)
		{
			_meshRenderer.sortingLayerID = SortingLayer;
			_meshRenderer.sortingOrder = OrderInLayer;
			_sortDirty = false;
			_prevSortLayer = SortingLayer;
			_prevSortOrder = OrderInLayer;
		}
	}

	private void DrawMesh()
	{
		if (_meshFilter.sharedMesh == null)
		{
			_meshFilter.sharedMesh = new Mesh();
			_meshFilter.sharedMesh.name = "2D Light Mesh";
		}

		_meshFilter.sharedMesh.Clear();
		ZeroOutArrays();

		if (Angle == 0)
			return;

		_vertBuffer[0] = Vector3.zero;
		_uvBuffer[0] = new Vector2(0.5f, 0.5f);
		_colorBuffer[0] = LightColor * Intensity;

		Vector3 lossyScale = transform.lossyScale;
		try
		{
			for (int v = 0; v < Mathf.Min(_raycastHits.Count, 1000); v++)
			{
				Vector3 localPoint = _raycastHits[v].point;
				_vertBuffer[v + 1] = localPoint;
				// UVs will ignore the lossyscale of the transform.
				_uvBuffer[v + 1] = new Vector3(localPoint.x / (Range * 2 / lossyScale.x) + 0.5f, localPoint.y / (Range * 2 / lossyScale.y) + 0.5f);
				_colorBuffer[v + 1] = _colorBuffer[0];

				if (CloseEnough(_raycastHits[v].angle, _maxAngle) && Angle != 360)
					continue;

				_triBuffer[v * 3 + 0] = 0;
				_triBuffer[v * 3 + 1] = v + 1;
				_triBuffer[v * 3 + 2] = v + 2;
			}

			_meshFilter.sharedMesh.vertices = _vertBuffer;
			_meshFilter.sharedMesh.uv = _uvBuffer;
			_meshFilter.sharedMesh.colors = _colorBuffer;
			_meshFilter.sharedMesh.triangles = _triBuffer;
		}
		catch
		{
			_meshFilter.sharedMesh.Clear();
			Debug.LogWarning(string.Format("Light2D \"{0}\" is attempting to create more than {1} verticies. Either reduce shadow collider complexity or increase MaxVertCount", name, MaxVertCount));
		}
	}

	private void LockRotation()
	{
		transform.eulerAngles = new Vector3(0, 0, transform.eulerAngles.z);
	}

	private void ZeroOutArrays()
	{
		System.Array.Clear(_vertBuffer, 0, _vertBuffer.Length);
		System.Array.Clear(_triBuffer, 0, _triBuffer.Length);
	}

	private void RegisterColliderInternal(Collider2D col)
	{
		_colliders.Add(col);
		_pointSets.Add(GetPoints(col));
	}

	/// <summary>
	/// Returns an array of verticies that represent the local space points of the Collider2D
	/// </summary>
	private Vector2[] GetPoints(Collider2D col)
	{
		if (col is PolygonCollider2D)
		{
			PolygonCollider2D polygon = (PolygonCollider2D)col;

			List<Vector2> points = new List<Vector2>();

			for (int i = 0; i < polygon.pathCount; i++)
				points.AddRange(polygon.GetPath(i));

			for (int i = 0; i < points.Count; i++)
				points[i] += polygon.offset;

			return points.ToArray();
		}

		else if (col is BoxCollider2D)
		{
			BoxCollider2D box = (BoxCollider2D)col;

			Vector2[] points = new Vector2[] {
				box.offset + new Vector2(-box.size.x / 2, +box.size.y / 2),
				box.offset + new Vector2(+box.size.x / 2, +box.size.y / 2),
				box.offset + new Vector2(-box.size.x / 2, -box.size.y / 2),
				box.offset + new Vector2(+box.size.x / 2, -box.size.y / 2)
			};

			return points;
		}

		else if (col is EdgeCollider2D)
		{
			EdgeCollider2D edge = (EdgeCollider2D)col;
			return edge.points;
		}

		else if (col is CircleCollider2D)
		{
			CircleCollider2D circle = (CircleCollider2D)col;
			List<Vector2> points = new List<Vector2>();
			Vector2 centre = circle.offset;

			int colliderResolution = 16;

			for (int i = 0; i < colliderResolution; i++)
			{
				float rad = (i / (float)colliderResolution) * Mathf.PI * 2;
				points.Add(centre + new Vector2(Mathf.Cos(rad) * circle.radius, Mathf.Sin(rad) * circle.radius));
			}

			return points.ToArray();
		}

		return null;
	}

	private int GetWorldPointsNonAlloc(int index, Vector2[] buffer)
	{
		if (_pointSets[index] == null)
			return 0;

		for (int i = 0; i < buffer.Length && i < _pointSets[index].Length; i++)
			buffer[i] = _colliders[index].transform.TransformPoint(_pointSets[index][i]);

		return Mathf.Min(buffer.Length, _pointSets[index].Length);
	}

	private void CastRays()
	{
		_raycastHits.Clear();

		if (Angle == 0)
			return;

		float zRot = transform.eulerAngles.z;

		/// Segment Rays ///
		///
		/// These are the rays that we cast out to fill in the shape of the light when there aren't shadow casters around.
		/// A really high segment value would hardly have need for the Collider Rays, but it would also have a lot of wasteful geometry.

		for (int s = 0; s < _segments + 1; s++)
		{
			float lerp = Mathf.Lerp(0, Angle, s / (float)_segments) - Angle / 2;

			Vector2 direction = (Vector2)(transform.TransformDirection(Quaternion.AngleAxis(lerp, -transform.forward) * (Vector3.up)));
			RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, Range, ShadowMask);
			Vector2 castPoint = (Vector2)transform.position + direction * Range;

			if (hit.collider == null)
			{
				PointAngle sortHit = new PointAngle
				{
					angle = -Mathf.Atan2((castPoint - (Vector2)transform.position).normalized.y - 1, (castPoint - (Vector2)transform.position).normalized.x),
					point = transform.InverseTransformPoint(castPoint)
				};

				_raycastHits.Add(sortHit);
			}

			else
			{
				PointAngle sortHit = new PointAngle
				{
					angle = -Mathf.Atan2((hit.point - (Vector2)transform.position).normalized.y - 1, (hit.point - (Vector2)transform.position).normalized.x),
					point = transform.InverseTransformPoint(hit.point)
				};

				_raycastHits.Add(sortHit);
			}

			if (s == _segments)
				_maxAngle = _raycastHits[_raycastHits.Count - 1].angle;
		}

		/// Edge Rays ///
		/// 
		/// These are the rays that capture objects on the edge of the light. It's not enough to cast to 
		/// colliders verticies, expecially in the case of thin triangles where a large part of the polygon's volume
		/// is in range of the light, but only a single vertex is in Range.

		_edgePoints.Clear();

		for (int s = 0; s < _segments; s++)
		{
			Vector3 a = transform.TransformDirection(Quaternion.AngleAxis(Mathf.Lerp(0, 360, s / (float)_segments), -transform.forward) * (Vector3.up));
			Vector3 b = transform.TransformDirection(Quaternion.AngleAxis(Mathf.Lerp(0, 360, (s + 1) / (float)_segments), -transform.forward) * (Vector3.up));
			Vector3 ab = (b - a).normalized;
			float l = 2 * Range * Mathf.Sin(Mathf.PI / _segments);

			int hitCount = Physics2D.RaycastNonAlloc(transform.position + a * Range, ab, _edgeHitBuffer, l, ShadowMask);

			for (int i = 0; i < hitCount; i++)
				_edgePoints.Add(_edgeHitBuffer[i].point);

			hitCount = Physics2D.RaycastNonAlloc(transform.position + b * Range, -ab, _edgeHitBuffer, l, ShadowMask);

			for (int i = 0; i < hitCount; i++)
				_edgePoints.Add(_edgeHitBuffer[i].point);
		}

		for (int i = 0; i < _edgePoints.Count; i++)
		{
			Vector2 direction = (_edgePoints[i] - (Vector2)transform.position).normalized;
			float angle = -Mathf.Atan2(direction.y - 1, direction.x);

			if (Angle != 360 && !IsBetween(angle * 2 * Mathf.Rad2Deg, -Angle / 2 - zRot, Angle / 2 - zRot))
				continue;

			RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, Range, ShadowMask);

			if (Vector2.Distance(hit.point, _edgePoints[i]) < 0.001f)
			{
				PointAngle sortHit = new PointAngle
				{
					angle = -Mathf.Atan2(direction.y - 1, direction.x),
					point = transform.InverseTransformPoint(_edgePoints[i])
				};

				_raycastHits.Add(sortHit);
			}
		}

		int removeIndex = -1;

		/// Collider Vertex Rays ///	
		for (int i = 0; i < _colliders.Count; i++)
		{
			if (_colliders[i] == null)
			{
				removeIndex = i;
				continue;
			}

			// Skip this point set altogether if the associated collider is not on a layer on the light's ShadowMask
			if ((ShadowMask.value & 1 << _colliders[i].gameObject.layer) == 0)
				continue;

			int pointCount = GetWorldPointsNonAlloc(i, _pointBuffer);

			for (int j = 0; j < pointCount; j++)
			{
				if (Vector2.Distance(transform.position, _pointBuffer[j]) > Range)
					continue;

				Vector2 direction = (_pointBuffer[j] - (Vector2)transform.position).normalized;
				float angle = -Mathf.Atan2(direction.y - 1, direction.x);

				if (Angle != 360 && !IsBetween(angle * 2 * Mathf.Rad2Deg, -Angle / 2 - zRot, Angle / 2 - zRot))
					continue;

				RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, Range, ShadowMask);

				if (hit.collider != null)
				{
					if (hit.collider != _colliders[i] && Vector2.SqrMagnitude((Vector2)transform.position - _pointBuffer[j]) > Vector2.SqrMagnitude((Vector2)transform.position - hit.point))
						continue;

					if (Vector2.Distance(hit.point, _pointBuffer[j]) < 0.001f)
					{
						PointAngle sortHit = new PointAngle
						{
							angle = -Mathf.Atan2(direction.y - 1, direction.x),
							point = transform.InverseTransformPoint(hit.point)
						};

						_raycastHits.Add(sortHit);
					}
				}

				else
				{
					PointAngle sortHit = new PointAngle
					{
						angle = -Mathf.Atan2(direction.y - 1, direction.x),
						point = transform.InverseTransformPoint(_pointBuffer[j])
					};

					_raycastHits.Add(sortHit);
				}

				/// Near Miss Rays ///
				/// 
				/// These are rays cast 'theta' radians clockwise and counter-clockwise of the target direction.
				/// Unfortunately we can't make assumptions that a vertex that is in Range and not obscured by another collider
				/// will necessarily have a raycast hit. It's just a floating point accuracy issue between our points and what Physics2D wants to do.

				float theta = 0.0005f;
				float cosTheta = Mathf.Cos(theta);
				float sinTheta = Mathf.Sin(theta);

				Vector2 v0 = new Vector2(direction.x * cosTheta - direction.y * sinTheta, direction.x * sinTheta + direction.y * cosTheta);

				cosTheta = Mathf.Cos(-theta);
				sinTheta = Mathf.Sin(-theta);
				Vector2 v1 = new Vector2(direction.x * cosTheta - direction.y * sinTheta, direction.x * sinTheta + direction.y * cosTheta);

				RaycastHit2D v0hit = Physics2D.Raycast(transform.position, v0, Range, ShadowMask);
				RaycastHit2D v1hit = Physics2D.Raycast(transform.position, v1, Range, ShadowMask);

				if (v0hit.collider != null)
				{
					PointAngle sortHit = new PointAngle
					{
						angle = -Mathf.Atan2(v0.y - 1, v0.x),
						point = transform.InverseTransformPoint(v0hit.point)
					};

					_raycastHits.Add(sortHit);
				}
				else
				{
					Vector2 edgePoint = (Vector2)transform.position + v0 * Range;

					PointAngle sortHit = new PointAngle
					{
						angle = -Mathf.Atan2(v0.y - 1, v0.x),
						point = transform.InverseTransformPoint(edgePoint)
					};

					_raycastHits.Add(sortHit);
				}

				if (v1hit.collider != null)
				{
					PointAngle sortHit = new PointAngle
					{
						angle = -Mathf.Atan2(v1.y - 1, v1.x),
						point = transform.InverseTransformPoint(v1hit.point)
					};

					_raycastHits.Add(sortHit);
				}
				else
				{
					Vector2 edgePoint = (Vector2)transform.position + v1 * Range;

					PointAngle sortHit = new PointAngle
					{
						angle = -Mathf.Atan2(v1.y - 1, v1.x),
						point = transform.InverseTransformPoint(edgePoint)
					};

					_raycastHits.Add(sortHit);
				}
			}
		}

		// Remove the list element at the flagged position.
		// One element is removed per frame and the others are skipped over.
		if (removeIndex != -1)
		{
			_colliders.RemoveAt(removeIndex);
			_pointSets.RemoveAt(removeIndex);
		}

		// The data are sorted by angle after all raycasting is done so triangles can be made by connecting the points in ascending order.
		_raycastHits.Sort((sh0, sh1) => sh0.angle.CompareTo(sh1.angle));

		// After the list is sorted, duplicate the first element at the end of the list
		// So that the light wraps around on the end; this is easier than reusing the vertex.
		_raycastHits.Add(_raycastHits[0]);
	}

	/// <summary>
	/// Returns true if an angle 'n' is between two angles 'a' and' b'.
	/// </summary>
	private bool IsBetween(float n, float a, float b)
	{
		n = (360 + (n % 360)) % 360;
		a = (3600000 + a) % 360;
		b = (3600000 + b) % 360;

		if (a < b)
			return a <= n && n <= b;

		return a <= n || n <= b;
	}

	/// <summary>
	/// Returns true when a float 'f' is within a certain range 'p' of target float 't'.
	/// </summary>
	private bool CloseEnough(float f, float t, float p = 0.001f)
	{
		return (f <= t + p && f >= t - p);
	}

	/// <summary>
	/// Avoid an in-editor memory leak by destroying the mesh when this light is destroyed.
	/// </summary>
	void OnDestroy()
	{
		if (_meshFilter.sharedMesh != null)
			DestroyImmediate(_meshFilter.sharedMesh);

		_manager.DeregisterLight(this);
	}

	/// <summary>
	/// Draw a fancy little Unity-esque gizmo for the light.
	/// </summary>
	void OnDrawGizmos()
	{
		Gizmos.DrawIcon(transform.position, "light_2d_gizmo.png", false);
	}
}