using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomEditor(typeof(Light2D)), CanEditMultipleObjects]
public class Light2DEditor : Editor
{
#pragma warning disable 0618
	[MenuItem("GameObject/Light/2D/Additive")]
	static void CreateNew2DLightAdd()
	{
		GameObject newLight = new GameObject();
		newLight.AddComponent<Light2D>();
		newLight.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Light2D-Additive");
		newLight.name = "2D Light";
	}

	[MenuItem("GameObject/Light/2D/Alpha")]
	static void CreateNew2DLightBlend()
	{
		GameObject newLight = new GameObject();
		newLight.AddComponent<Light2D>();
		newLight.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Light2D-Alpha Blend");
		newLight.name = "2D Light";
	}

	void OnSceneGUI()
	{
		Light2D tar = (Light2D)target;
		EditorUtility.SetSelectedWireframeHidden(tar.GetComponent<MeshRenderer>(), true);

		Undo.RecordObject(tar, "Edit Target Light");
		Undo.RecordObject(tar.transform, tar.transform.GetHashCode() + "_undo");

		Color color = ((tar.LightColor + Color.white) / 2);
		color.a = 1;
		Handles.color = color;

		Handles.DrawWireArc(tar.transform.position, tar.transform.forward, Quaternion.AngleAxis(180 - tar.Angle / 2, tar.transform.forward) * -tar.transform.up, tar.Angle, tar.Range);

		DrawRangeHandles(tar);
		DrawAngleHandles(tar);
		DrawRotationHandle(tar);

		if (GUI.changed)
			EditorUtility.SetDirty(tar);
	}

	void DrawRangeHandles(Light2D tar)
	{
		float r = tar.Range;

		for (int i = 0; i < 5; i++)
		{
			Vector3 direction = (Quaternion.AngleAxis((float)i / 4 * tar.Angle - tar.Angle / 2, -tar.transform.forward) * tar.transform.up).normalized;
			r = (tar.transform.position - Handles.FreeMoveHandle(
				tar.transform.position + direction * r,
				Quaternion.identity,
				HandleUtility.GetHandleSize(tar.transform.position) * 0.035f, Vector3.zero, Handles.DotHandleCap))
				.magnitude;
		}

		r = Mathf.Round(r * 1000) / 1000f;
		tar.Range = r;
	}

	void DrawAngleHandles(Light2D tar)
	{
		bool angleDirty = false;

		#region Handles
		Vector3 cwPos = tar.transform.position + (Quaternion.AngleAxis(-tar.Angle / 2, -tar.transform.forward) * (tar.transform.up)) * (tar.Range + HandleUtility.GetHandleSize(tar.transform.position) * 0.3f);
		Vector3 cwBasePos = tar.transform.position + (Quaternion.AngleAxis(-tar.Angle / 2, -tar.transform.forward) * (tar.transform.up)) * tar.Range;
		Vector3 cwHandle = Handles.FreeMoveHandle(cwPos, Quaternion.identity, HandleUtility.GetHandleSize(tar.transform.position) * .06f, Vector3.zero, Handles.CircleHandleCap);
		Vector3 toCwHandle = (tar.transform.position - cwHandle).normalized;
		Handles.DrawLine(cwBasePos, cwPos - (cwPos - cwBasePos).normalized * HandleUtility.GetHandleSize(tar.transform.position) * 0.05f);

		if (GUIUtility.hotControl == GetLastControlId())
		{
			tar.Angle = 360 - 2 * Quaternion.Angle(Quaternion.FromToRotation(tar.transform.up, toCwHandle), Quaternion.identity);
			angleDirty = true;
		}

		Vector3 ccwPos = tar.transform.position + (Quaternion.AngleAxis(tar.Angle / 2, -tar.transform.forward) * (tar.transform.up)) * (tar.Range + HandleUtility.GetHandleSize(tar.transform.position) * 0.3f);
		Vector3 ccwBasePos = tar.transform.position + (Quaternion.AngleAxis(tar.Angle / 2, -tar.transform.forward) * (tar.transform.up)) * tar.Range;
		Vector3 ccwHandle = Handles.FreeMoveHandle(ccwPos, Quaternion.identity, HandleUtility.GetHandleSize(tar.transform.position) * .06f, Vector3.zero, Handles.CircleHandleCap);
		Vector3 toCcwHandle = (tar.transform.position - ccwHandle).normalized;
		Handles.DrawLine(ccwBasePos, ccwPos - (ccwPos - ccwBasePos).normalized * HandleUtility.GetHandleSize(tar.transform.position) * 0.05f);

		if (GUIUtility.hotControl == GetLastControlId())
		{
			tar.Angle = 360 - 2 * Quaternion.Angle(Quaternion.FromToRotation(tar.transform.up, toCcwHandle), Quaternion.identity);
			angleDirty = true;
		}
		#endregion

		tar.Angle = Mathf.Round(tar.Angle * 100) / 100f;

		// Hold Ctrl for snapping
		Event e = Event.current;
		if (e.control && angleDirty)
			tar.Angle = Mathf.Round(tar.Angle / EditorPrefs.GetFloat("RotationSnap")) * EditorPrefs.GetFloat("RotationSnap");
	}

	void DrawRotationHandle(Light2D tar)
	{
		float r = Mathf.Min(HandleUtility.GetHandleSize(tar.transform.position) * 0.6f, tar.Range / 2);
		Vector3 handlePos = Handles.FreeMoveHandle(tar.transform.position - tar.transform.up * r, Quaternion.identity, HandleUtility.GetHandleSize(tar.transform.position) * .06f, Vector3.zero, Handles.CircleHandleCap);

		if (GUIUtility.hotControl == GetLastControlId())
			tar.transform.up = (tar.transform.position - handlePos).normalized;

		Event e = Event.current;
		if (e.control && (GUIUtility.hotControl == GetLastControlId()))
			tar.transform.eulerAngles = Vector3.forward * Mathf.Round(tar.transform.eulerAngles.z / EditorPrefs.GetFloat("RotationSnap")) * EditorPrefs.GetFloat("RotationSnap");

		#region Handle Visual
		for (int i = 0; i < 16; i++)
			Handles.DrawWireArc(tar.transform.position, Vector3.forward, Quaternion.AngleAxis((float)i / 16 * 360, Vector3.forward) * Vector3.up, 16.0f, r);

		Handles.DrawWireArc(tar.transform.position - tar.transform.up * r, Vector3.forward, Vector3.left + Vector3.down * 0.5f, -100, HandleUtility.GetHandleSize(tar.transform.position) * .14f);
		Handles.DrawWireArc(tar.transform.position - tar.transform.up * r, Vector3.forward, Vector3.right + Vector3.up * 0.5f, -100, HandleUtility.GetHandleSize(tar.transform.position) * .14f);

		Vector3 arrowEnd0 = tar.transform.position - tar.transform.up * r + (Vector3.right * 0.3f + Vector3.down) * HandleUtility.GetHandleSize(tar.transform.position) * .135f;
		Handles.DrawLine(arrowEnd0, arrowEnd0 + (Vector3.right * 0.4f + Vector3.up).normalized * HandleUtility.GetHandleSize(tar.transform.position) * .08f);
		Handles.DrawLine(arrowEnd0, arrowEnd0 + (Vector3.right + Vector3.down * 0.2f).normalized * HandleUtility.GetHandleSize(tar.transform.position) * .08f);
		Vector3 arrowEnd1 = tar.transform.position - tar.transform.up * r + (Vector3.left * 0.3f + Vector3.up) * HandleUtility.GetHandleSize(tar.transform.position) * .135f;
		Handles.DrawLine(arrowEnd1, arrowEnd1 + (Vector3.left + Vector3.up * 0.2f).normalized * HandleUtility.GetHandleSize(tar.transform.position) * .08f);
		Handles.DrawLine(arrowEnd1, arrowEnd1 + (Vector3.left * 0.4f + Vector3.down).normalized * HandleUtility.GetHandleSize(tar.transform.position) * .08f);
		#endregion
	}

	// Reflection hack to get the control ID of the last assigned handle.
	//
	// Useful because the FreeMove handles governing rotation and angle sometime mess up Undo if they update constantly.
	// Now we can always update the handle, but only feed that value back into the rotation/angle when the Handle is actually being used.
	public static FieldInfo LastControlIdField = typeof(EditorGUIUtility).GetField("s_LastControlID", BindingFlags.Static | BindingFlags.NonPublic);
	public static int GetLastControlId()
	{
		if (LastControlIdField == null)
		{
			Debug.LogError("Compatibility with Unity broke: can't find lastControlId field in EditorGUI");
			return 0;
		}
		return (int)LastControlIdField.GetValue(null);
	}
#pragma warning restore 0618
}
