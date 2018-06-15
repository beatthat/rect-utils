using BeatThat.Pools;
using BeatThat.TransformPathExt;
using UnityEngine;

namespace BeatThat.Rects
{
    public static class RectTransformExtensions 
	{
		#if UNITY_EDITOR
		public static void DrawGizmoScreenRect(this RectTransform rt, Color color)
		{
			rt.DrawGizmoScreenRect (rt.GetScreenRect (), color);
		}

		public static void DrawGizmoScreenRect(this RectTransform rt, Rect screenRect, Color color)
		{
			var saveColor = Gizmos.color;

			var r = rt.TransformScreenRect(screenRect);

			Gizmos.color = color;

			Gizmos.DrawLine((Vector3)r.min, new Vector3(r.xMin, r.yMax));
			Gizmos.DrawLine(new Vector3(r.xMin, r.yMax), (Vector3)r.max);
			Gizmos.DrawLine((Vector3)r.max, new Vector3(r.xMax, r.yMin));
			Gizmos.DrawLine(new Vector3(r.xMax, r.yMin), (Vector3)r.min);

			Gizmos.color = saveColor;
		}

		public static void DrawGizmoFillScreenRect(this RectTransform rt, Color color)
		{
			rt.DrawGizmoFillScreenRect (rt.GetScreenRect (), color);
		}

		public static void DrawGizmoFillScreenRect(this RectTransform rt, Rect screenRect, Color color)
		{
			var saveColor = Gizmos.color;

			var r = rt.TransformScreenRect(screenRect);

			Gizmos.color = color;

			Gizmos.DrawCube(r.center, r.size);

			Gizmos.color = saveColor;
		}
		#endif

		public static bool ScreenPointToWorldPoint(this RectTransform rt, Vector2 screenPoint, out Vector3 worldPoint, Canvas canvas = null) 
		{
			if(canvas == null) {
				canvas = rt.GetComponentInParent<Canvas>();
			}

			if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace) {
				return RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, screenPoint, canvas.worldCamera, out worldPoint);
			}
			else {
				return RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, screenPoint, null, out worldPoint);
			}
		}

		/// <summary>
		/// Gets the aspect ratio (width/height) of the screen rect for a RectTransform
		/// </summary>
		public static float GetScreenAspect(this RectTransform rt, Canvas canvas = null) 
		{
			var screenRect = rt.GetScreenRect(canvas);

			if(screenRect.height <= 0f) {
				Debug.LogWarning("[" + Time.frameCount + "] RectTransformExtensions::GetScreenAspect height is not a positive number");
				return float.NaN;
			}

			return screenRect.width/screenRect.height;
		}

		public static bool ContainsScreenPoint(this RectTransform rt, Vector2 screenPoint)
		{
			return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPoint);
		}

		/// <summary>
		/// Changes a RectTransform to assume the screen rect of its target
		/// and then also make the target its child with anchors pinned to the corners.
		/// 
		/// This is useful when a component wants to take ownership of some external object
		/// and transition it into place.
		/// </summary>
		/// <param name="thisRt">This rt.</param>
		/// <param name="target">Target.</param>
		public static void BecomeParentAndDriverOf(this RectTransform thisRt, RectTransform target)
		{
			thisRt.SetScreenRect(target.GetScreenRect());
			target.SetParent(thisRt.transform, true);
			target.anchorMin = Vector2.zero;
			target.anchorMax = Vector2.one;
			target.offsetMin = Vector2.zero;
			target.offsetMax = Vector2.zero;
		}
			
		/// <summary>
		/// Gets the screen rect (pixel rect) for a RectTransform.
		/// 
		/// NOTE: does NOT work if rect has rotation applied
		/// 
		/// </summary>
		/// <returns>The screen rect.</returns>
		/// <param name="rt">Rect transform.</param>
		/// <param name="canvas">Canvas.</param>
		public static Rect GetScreenRect(this RectTransform rt, Canvas canvas = null) 
		{
			if(rt.eulerAngles != Vector3.zero) {
				#if UNITY_EDITOR || BT_DEBUG_UNSTRIP
				Debug.LogWarning("[" + Time.frameCount + "][" + rt.Path() + "] GetScreenRect does not work on rotate RectTransform eulerAngles=" + rt.eulerAngles);
				#endif
			}

			if(canvas == null) {
				canvas = rt.GetComponentInParent<Canvas>();
			}

			using(var cornersArr = ArrayPool<Vector3>.Get(4)) {
				using(var screenCornersArr = ArrayPool<Vector3>.Get(2)) {
					
					var corners = cornersArr.array;
					var screenCorners = screenCornersArr.array;

					rt.GetWorldCorners(corners);

					if (canvas != null && (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)) {
						screenCorners[0] = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]);
						screenCorners[1] = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[2]);
					}
					else {
						screenCorners[0] = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
						screenCorners[1] = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
					}

					return new Rect(screenCorners[0], screenCorners[1] - screenCorners[0]);
				}
			}
		}

		/// <summary>
		/// Update the anchors of a RectTransform without changing its screen rect
		/// </summary>
		/// <param name="anchorMax">Anchor max.</param>
		public static void SetAnchorsWithCurrentScreenRect(this RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
		{
			var screenRect = rt.GetScreenRect();

			if(anchorMax.x - anchorMin.x < .01f || anchorMax.y - anchorMin.y < 0.1f) {
				// avoid collapsing the rect by giving it a temp size
				rt.sizeDelta = new Vector2(1f, 1f);
			}


			rt.anchorMax = anchorMax;
			rt.anchorMin = anchorMin;

			rt.SetScreenRect(screenRect);
		}

		/// <summary>
		/// Gets the screen rect (pixel rect) for a RectTransform
		/// </summary>
		/// <returns>The screen rect.</returns>
		/// <param name="rt">The extension-method 'this' RectTransform to which the new screen rect will be applied</param>
		/// <param name = "tgtScreenRect">The screen coords to apply</param>
		/// <param name="canvas">Canvas.</param>
		public static void SetScreenRect(this RectTransform rt, Rect tgtScreenRect, Canvas canvas = null) 
		{
			if(canvas == null) {
				canvas = rt.GetComponentInParent<Canvas>();
			}


			var tgtValid = (tgtScreenRect.width > 0f && tgtScreenRect.height > 0f);

//			Assert.IsTrue(tgtValid, "[" + rt.Path() + "] attempt to set rect to collapsed or inverted shape (not supported)");

			if(!tgtValid) {
				Debug.LogWarning("[" + rt.Path() + "] attempt to set rect to collapsed or inverted shape (not supported)");
				return;
			}

			var curScreenRect = rt.GetScreenRect(canvas);

			var curValid = (curScreenRect.width > 0f && curScreenRect.height > 0f);
//			Assert.IsTrue(curValid, "[" + rt.Path() + "] attempt to set rect to collapsed or inverted shape (not supported)");

			if(!curValid) {
				rt.sizeDelta = Vector2.one; // try to recover by giving rect a 1 unit size...
				curScreenRect = rt.GetScreenRect(canvas);
				curValid = (curScreenRect.width > 0f && curScreenRect.height > 0f);
				if(!curValid) {
					Debug.LogWarning("[" + rt.Path() + "] Not implemented to work oncollapsed or inverted rect " + curScreenRect);
					return;
				}
			}

			if(curScreenRect.Approximately(tgtScreenRect)) {
				return;
			}

			var cam = (canvas != null && (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace))? canvas.worldCamera: null;

			var scaleFactor = new Vector2(tgtScreenRect.width/curScreenRect.width, tgtScreenRect.height/curScreenRect.height);
			if(scaleFactor != Vector2.one) {
				rt.ScaleBy(scaleFactor, false);
				curScreenRect = rt.GetScreenRect(canvas);
			}

			Vector2 curCenter, tgtCenter;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(rt.parent as RectTransform, tgtScreenRect.center, cam, out tgtCenter);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(rt.parent as RectTransform, curScreenRect.center, cam, out curCenter);
			var dPos = (Vector2)tgtCenter - (Vector2)curCenter;
			rt.anchoredPosition += dPos;
		}

		/// <summary>
		/// Scale a RectTransform by changing it's size (while leaving scale alone)
		/// </summary>
		/// <param name="rt">The rect transform ('this' in extension method)</param>
		/// <param name = "scaleFactor">The factor by which to scale</param>
		/// <param name = "adjustAnchoredPosition">If TRUE will adjust the anchored position of the RectTransform by the scaleFactor. Default is TRUE</param>
		public static void ScaleBy(this RectTransform rt, Vector2 scaleFactor, bool adjustAnchoredPosition = true) 
		{
			var r = rt.rect;
			rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, r.width * scaleFactor.x);
			rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, r.height * scaleFactor.y);

			if(adjustAnchoredPosition) {
				rt.anchoredPosition = new Vector2(rt.anchoredPosition.x * scaleFactor.x, rt.anchoredPosition.y * scaleFactor.y);
			}
		}

		/// <summary>
		/// Scale a RectTransform by changing it's size (while leaving scale alone)
		/// </summary>
		/// <param name="rt">The rect transform ('this' in extension method)</param>
		/// <param name = "scaleFactor">The factor by which to scale</param>
		/// <param name = "adjustAnchoredPosition">If TRUE will adjust the anchored position of the RectTransform by the scaleFactor. Default is TRUE</param>
		public static void ScaleBy(this RectTransform rt, float scaleFactor, bool adjustAnchoredPosition = true) 
		{
			rt.ScaleBy(new Vector2(scaleFactor, scaleFactor), adjustAnchoredPosition);
		}

		/// <summary>
		/// Calculates the scale factor that, when applied, would produce the given world height,
		/// e.g. if the current world height is 100 and the new world height is 50, then returns 0.5f
		/// </summary>
		/// <returns>The factor to world height.</returns>
		/// <param name="rt">Rt.</param>
		/// <param name="targetWorldHeight">World height.</param>
		public static float ScaleFactorToWorldHeight(this RectTransform rt, float targetWorldHeight) 
		{
			var localHeight = Mathf.Abs(rt.InverseTransformVector(new Vector3(0f, targetWorldHeight, 0f)).magnitude);
			return localHeight/rt.rect.height;
		}

		/// <summary>
		/// Calculates the scale factor that, when applied, would produce the given screen height,
		/// e.g. if the current screen height is 100 and the new world height is 50, then returns 0.5f
		/// </summary>
		/// <returns>The factor to screen height.</returns>
		/// <param name="rt">Rt.</param>
		/// <param name="tgtScreenHeight">screen height.</param>
		public static float ScaleFactorToScreenHeight(this RectTransform rt, float tgtScreenHeight) 
		{
			var curHeight = rt.GetScreenRect().height;
			return tgtScreenHeight/curHeight;
		}

		/// <summary>
		/// Scale a RectTransform to a given world height by changing it's width and height (while leave actual scale alone)
		/// </summary>
		public static void ScaleToWorldHeight(this RectTransform rt, float targetWorldHeight) 
		{
			rt.ScaleBy(rt.ScaleFactorToWorldHeight(targetWorldHeight));
		}

		/// <summary>
		/// Scale a RectTransform to a given screen height by changing it's width and height (while leave actual scale alone)
		/// </summary>
		public static void ScaleToScreenHeight(this RectTransform rt, float tgtScreenHeight) 
		{
			rt.ScaleBy(rt.ScaleFactorToScreenHeight(tgtScreenHeight));
		}
			
		public static Rect GetWorldRect(this RectTransform rt) 
		{
			using(var cornersArr = ArrayPool<Vector3>.Get(4)) {
				var corners = cornersArr.array;
				rt.GetWorldCorners(corners);
				return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
			}
		}


		/// <summary>
		/// Makes a RectTransform fill the full space of another rect transform.
		/// Mostly for creating test objects. 
		/// </summary>
		public static RectTransform FillSpaceOf(this RectTransform rt, RectTransform rt2)
		{
//			Debug.LogError("["+ Time.frameCount + "] RectTransform[" + rt.name + "]::FillSpace of " + rt2.name);

//			var parentRect = r.InverseTransformRect(r.parent as RectTransform);
			var tgtRect = rt.InverseTransformRect(rt2);

			rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tgtRect.width);
			rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tgtRect.height);

			var rect = rt.rect;
			rt.anchoredPosition += new Vector2(tgtRect.xMin - rect.xMin, tgtRect.yMin - rect.yMin);
			return rt;
		}

		/// <summary>
		/// Makes a RectTransform fill the full space of its parent.
		/// Mostly for creating test objects. 
		/// </summary>
		public static RectTransform FillParentSpace(this RectTransform r)
		{
			r.FillSpaceOf(r.parent as RectTransform);
			return r;
		}

		/// <summary>
		/// Variation of RectTransform::SetInsetAndSizeFromParentEdge that clamps the resulting edge
		/// to ensure that it's contained within the parent
		/// </summary>
		public static void SetInsetAndSizeFromParentEdgeInterior(this RectTransform rt, RectTransform.Edge edge, float inset, float size)
		{
			var pSize = (edge == RectTransform.Edge.Bottom || edge == RectTransform.Edge.Top)? rt.rect.height: rt.rect.width;

			// clamp the inset so that it is neither negative (past the parent edge) 
			// nor greater than the parent's size (past the opposite parent edge)
			var insetClamped = Mathf.Clamp(inset, 0f, pSize); 

			// if the inset has been changed by clamping, adjust the size accordingly
			// and also so that it is not greater than the parent size 
			var sizeAdjusted = Mathf.Clamp(size - Mathf.Abs(inset - insetClamped), 0f, pSize);
			rt.SetInsetAndSizeFromParentEdge(edge, insetClamped, sizeAdjusted);
		}

		public static Rect InverseTransformRect(this RectTransform rt, RectTransform rt2)
		{
			Rect r;
			var c = rt.GetComponentInParent<Canvas>();
			if (c == null || !c.pixelPerfect) {
				r = rt2.rect;
			}
			else {
				r = RectTransformUtility.PixelAdjustRect(rt2, c);
			}

			var p =  rt2.TransformPoint(new Vector2(r.x, r.y));
			p = rt.InverseTransformPoint(p);

			r.x = p.x;
			r.y = p.y;

			return r;
		}

		/// <summary>
		/// Transform a rect from the ScreenSpace to world space
		/// </summary>
		/// <returns>The Rect in world coords</returns>
		/// <param name="screenRect">Rect in screen coords/param>
		public static Rect TransformScreenRect(this RectTransform rt, Rect screenRect, Canvas canvas = null)
		{
			if(canvas == null) {
				canvas = rt.GetComponentInParent<Canvas>();
			}

			if(canvas == null) {
				Debug.LogWarning("[" + Time.frameCount + "][" + rt.Path() + " unable to find parent canvas");
			}

			var cam = canvas != null && (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)? canvas.worldCamera: null;

			Vector2 localMin, localMax;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenRect.min, cam, out localMin);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenRect.max, cam, out localMax);

			return rt.TransformRect(Rect.MinMaxRect(localMin.x, localMin.y, localMax.x, localMax.y));
		}

		/// <summary>
		/// Transform a rect from the RectTransform's coordinate space to world space
		/// </summary>
		/// <returns>The Rect in world coords</returns>
		/// <param name="r">Rect in coord space of the RectTransform</param>
		public static Rect TransformRect(this RectTransform rt, Rect r)
		{
			var min = rt.TransformPoint(r.min);
			var max = rt.TransformPoint(r.max);
			r.min = min;
			r.max = max;
			return r;
		}

	}
}



