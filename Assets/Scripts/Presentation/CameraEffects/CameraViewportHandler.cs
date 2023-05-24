using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CameraViewportHandler : MonoBehaviour
{

	public enum Constraint { Overflow, UnderFlow, Portrait, Landscape }


	public Vector2 UnitsSize = new Vector2(1920, 1080);
	public Constraint constraint = Constraint.Overflow;

	public Camera Camera { get; private set; }
	[System.NonSerialized] private Vector2 _lastScreenSize;

	private void Awake()
	{
		Camera = GetComponent<Camera>();
	}

	private void Update()
	{
		if (Screen.width != _lastScreenSize.x || Screen.height != _lastScreenSize.y)
		{
			_lastScreenSize.x = Screen.width;
			_lastScreenSize.y = Screen.height;
			ComputeResolution();
		}
	}

	private void ComputeResolution()
	{
#if UNITY_EDITOR
		if (Camera == null)
			Camera = GetComponent<Camera>();

		if (Camera == null)
			throw new System.Exception("CameraViewportHandler requires a Camera component");
#endif

		// Landscape
		float landscape = 1f / Camera.aspect * UnitsSize.x / 2f;
		// Portrait
		float portrait = UnitsSize.y / 2f;

		switch (constraint)
		{
			case Constraint.Overflow:
				Camera.orthographicSize = Mathf.Max(landscape, portrait);
				break;
			case Constraint.UnderFlow:
				Camera.orthographicSize = Mathf.Min(landscape, portrait);
				break;
			case Constraint.Portrait:
				Camera.orthographicSize = portrait;
				break;
			case Constraint.Landscape:
				Camera.orthographicSize = landscape;
				break;
		}
	}


#if UNITY_EDITOR
	private void OnValidate()
	{
		ComputeResolution();
	}
#endif
}