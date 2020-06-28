using UnityEngine;
using UnityEditor;

/// <summary>
/// To facilitate the need for CircleColliders with Light2D,
/// this provides a helpful method of converting CircleColliders
/// to PolygonColliders with variable resolution.
/// </summary>
public class CircleToPolygon : ScriptableWizard
{
	public int ColliderResolution = 16;

	[MenuItem("Light2D/Convert Circle2D to Polygon2D")]
	static void CreateWizard()
	{
		if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<CircleCollider2D>())
			DisplayWizard<CircleToPolygon>("Convert Circle2D to Polygon2D", "Apply");
		else
			Debug.Log("Select a GameObject with a CircleCollider2D to reaplce.");
	}

	void OnWizardCreate()
	{
		if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<CircleCollider2D>())
		{
			CircleCollider2D circle = Selection.activeGameObject.GetComponent<CircleCollider2D>();
			PolygonCollider2D poly = Undo.AddComponent<PolygonCollider2D>(Selection.activeGameObject);

			Vector2[] points = new Vector2[ColliderResolution];
			for (int i = 0; i < ColliderResolution; i++)
				points[i] = Quaternion.Euler(Vector3.back *(i/(float)ColliderResolution) * 360) * Vector2.up * circle.radius;

			poly.points = points;
			poly.offset = circle.offset;

			Undo.DestroyObjectImmediate(circle);
        }
	}
}
