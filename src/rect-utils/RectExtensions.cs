using UnityEngine;
using System;
using UnityEngine.Assertions;

namespace BeatThat
{
	public enum RectConstraintsAdjustment { NONE = 0, ADJUSTED_TO_MEET_CONSTRAINTS = 1, ADJUSTED_BUT_FAILED_TO_MEET_CONSTRAINTS = 2 }

	[Flags]
	public enum EdgeFlags
	{
		None = 0, Left = 1, Top = 2, Right = 4, Bottom = 8
	}

	/// <summary>
	/// Utils and extension functions for unity's Rect
	/// </summary>
	public static class RectExtensions 
	{
		/// <summary>
		/// Linear interpolaton between 2 Rects
		/// </summary>
		public static Rect LerpTo(this Rect r, Rect r2, float pct)
		{            
			pct = Mathf.Clamp01(pct);

			return new Rect(Mathf.Lerp(r.x, r2.x, pct), Mathf.Lerp(r.y, r2.y, pct),
				Mathf.Lerp(r.width, r2.width, pct), Mathf.Lerp(r.height, r2.height, pct));
		}

		public static Rect LerpToUnclamped(this Rect r, Rect r2, float pct)
		{           
			return new Rect(Mathf.LerpUnclamped(r.x, r2.x, pct), Mathf.LerpUnclamped(r.y, r2.y, pct),
				Mathf.LerpUnclamped(r.width, r2.width, pct), Mathf.LerpUnclamped(r.height, r2.height, pct));
		}

		public static float Area(this Rect r)
		{            
			return r.width * r.height;
		}

		public static bool Contains(this Rect r, Rect r2)
		{            
			return (r.Contains(r2.min) && r.Contains(r2.max));
		}

		public static Rect ToRect(this Vector4 v)
		{
			return new Rect(v.x, v.y, v.z, v.w);
		}

		/// <summary>
		/// Get the corners of the rect: [bottomLeft, topLeft, topRight, bottomRight]
		/// </summary>
		/// <param name="corners">Corners.</param>
		public static void GetCorners(this Rect r, Vector2[] corners)
		{
			Assert.AreEqual(4, corners.Length);

			corners[0] = r.min;
			corners[1] = new Vector2(r.xMin, r.yMax);
			corners[2] = r.max;
			corners[3] = new Vector2(r.xMax, r.yMin);
		}

		/// <summary>
		/// Gets the edges (if any) that are colinear with point p. 
		/// For the case of a corner, returns both adjacent edges, e.g. topRight = EdgeFlags.Top | EdgeFlags.Right
		/// </summary>
		/// <returns>The edges.</returns>
		/// <param name="r">The rect.</param>
		/// <param name="p">The position to test</param>
		public static EdgeFlags GetEdges(this Rect r, Vector2 p)
		{
			var edges = EdgeFlags.None;

			if(Mathf.Approximately(p.x, r.xMin)) {
				edges |= EdgeFlags.Left;
			}

			if(Mathf.Approximately(p.x, r.xMax)) {
				edges |= EdgeFlags.Right;
			}

			if(Mathf.Approximately(p.y, r.yMax)) {
				edges |= EdgeFlags.Top;
			}

			if(Mathf.Approximately(p.y, r.yMin)) {
				edges |= EdgeFlags.Bottom;
			}

			return edges;
		}

		/// <summary>
		/// Calculate a Rect in viewport coordinates of a second Rect
		/// </summary>
		/// <returns>The Rect in viewport coordinates</returns>
		/// <param name="r">The 'this' Rect calling the extension function to be converted into viewport coordinates</param>
		/// <param name="r2">The rect to use as the reference</param>
		public static Rect InViewportCoordinatesOf(this Rect r, Rect r2)
		{
			if(r2.width <= 0 || r2.height <= 0f) {
				Debug.LogWarning("[" + Time.frameCount + "]RectExtensions::InViewportCoordinatesOf r2 is collapsed: " + r2);
				return new Rect(0f, 0f, 1f, 1f);
			}

			return new Rect((r.x - r2.x) / r2.width, (r.y - r2.y) / r2.height,
				r.width / r2.width, r.height / r2.height);
		}

		/// <summary>
		/// Bounds-type function applied to Rect: encapsulates a rect so that the returned Rect contains r and r2.
		/// </summary>
		public static Rect Encapsulate(this Rect r, Rect r2)
		{
			var res = new Rect();
			res.xMin = Mathf.Min(r.xMin, r2.xMin);
			res.yMin = Mathf.Min(r.yMin, r2.yMin);
			res.xMax = Mathf.Max(r.xMax, r2.xMax);
			res.yMax = Mathf.Max(r.yMax, r2.yMax);
			return res;
		}

		public static float GetAspect(this Rect r)
		{
			if(r.height <= 0) {
				Debug.LogWarning("[" + Time.frameCount + "] RectExtensions::GetAspect height is not positive");
				return float.NaN;
			}

			return r.width/r.height;
		}

		public static bool Intersects(this Rect r, Rect r2, out Rect intersection)
		{
			if(r.Covers(r2)) {
				intersection = r2;
				return true;
			}

			if(r2.Covers(r)) {
				intersection = r;
				return true;
			}

			if(r.Overlaps(r2)) {
				intersection = Rect.MinMaxRect(
					Mathf.Max(r.xMin, r2.xMin),
					Mathf.Max(r.yMin, r2.yMin),
					Mathf.Min(r.xMax, r2.xMax),
					Mathf.Min(r.yMax, r2.yMax));

				return true;
			}

			intersection = new Rect();
			return false;
		}
		

		/// <summary>
		/// Like contains except allowing covered (contained) rect to share an edge with the covering/containing rect.
		/// </summary>
		public static bool Covers(this Rect r, Rect r2, float epsilon = 0.0001f)
		{          
			if(r2.xMin < r.xMin - epsilon) {
				return false;
			}

			if(r2.xMax > r.xMax + epsilon) {
				return false;
			}

			if(r2.yMin < r.yMin - epsilon) {
				return false;
			}

			if(r2.yMax > r.yMax + epsilon) {
				return false;
			}

			return true;
		}

		public static bool Covers(this Rect r, Vector2 point, float epsilon = 0.0001f)
		{          
			if(point.x < r.xMin - epsilon) {
				return false;
			}

			if(point.x > r.xMax + epsilon) {
				return false;
			}

			if(point.y < r.yMin - epsilon) {
				return false;
			}

			if(point.y > r.yMax + epsilon) {
				return false;
			}

			return true;
		}

		/// <summary>
		/// If the point p is outside the rect, returns the closest point on the rect.
		/// NOTE this assumes the rect is axis aligned
		/// </summary>
		/// <param name="r">The rect to clamp to</param>
		/// <param name="p">The point</param>
		public static bool Clamp(this Rect r, ref Vector3 p)
		{
			if(r.Contains(p)) {
				return false;
			}
				
			var center = (Vector3)r.center;
			center.z = p.z;

			var b = new Bounds(center, new Vector3(r.width, r.height, 0.00001f));
			p = b.ClosestPoint(p);
			return true;
		}

		/// <summary>
		/// Check if a line (given by endpoints) intersects a rect.
		/// For these purposes, if the line is fully contained within the rect will be considered intersecting.
		/// Returns the resulting contained/intersecting line via out params
		/// </summary>
		/// <returns><c>true</c>, if line was intersectsed, <c>false</c> otherwise.</returns>
		/// <param name="r">The rect</param>
		/// <param name="p1">line end point 1</param>
		/// <param name="p2">line end point 2</param>
		/// <param name="p1WithinRect">result line endpoint 1</param>
		/// <param name="p2WithinRect">result line endpoint 2</param>
		public static bool IntersectsLine(this Rect r, Vector3 p1, Vector3 p2, out Vector3 p1WithinRect, out Vector3 p2WithinRect)
		{
			p1WithinRect = p1;
			p2WithinRect = p2;

			var p1Inside = r.Contains(p1);
			var p2Inside = r.Contains(p2);

			if(p1Inside && p2Inside) {
				return true;
			}

			var intersects = false;

			var len = Vector2.Distance((Vector2)p1, (Vector2)p2);

			float dist;
			var b = new Bounds(r.center, new Vector3(r.width, r.height, 0.0001f));

			if(!p1Inside) {
				if(b.IntersectRay(new Ray((Vector2)p1, (Vector2)(p2 - p1)), out dist) && dist <= len) {
					p1WithinRect = Vector3.Lerp(p1, p2, dist/len);
					intersects = true;
				}
			}

			if(!p2Inside) {
				if(b.IntersectRay(new Ray((Vector2)p2, (Vector2)(p1 - p2)), out dist) && dist <= len) {
					p2WithinRect = Vector3.Lerp(p2, p1, dist/len);
					intersects = true;
				}
			}

			return intersects;
		}

		public static bool IsOnEdge(this Rect r, Vector2 point, float epsilon = 0.0001f)
		{          
			if(Mathf.Abs(point.x - r.xMin) <= epsilon) {
				return true;
			}

			if(Mathf.Abs(point.x - r.xMax) <= epsilon) {
				return true;
			}

			if(Mathf.Abs(point.y - r.yMin) <= epsilon) {
				return true;
			}

			if(Mathf.Abs(point.y - r.yMax) <= epsilon) {
				return true;
			}

			return false;
		}


		public static bool Approximately(this Rect r, Rect r2, float epsilon = 0.0001f)
		{
			if(Mathf.Abs(r.x - r2.x) > epsilon) {
				return false;
			}

			if(Mathf.Abs(r.y - r2.y) > epsilon) {
					return false;
			}

			if(Mathf.Abs(r.width - r2.width) > epsilon) {
					return false;
			}

			if(Mathf.Abs(r.height - r2.height) > epsilon) {
					return false;
			}

			return true;
		}


		/// <summary>
		/// Clamp Rect r2 to Rect r1 in a way that ensures their intersection meets min width and min height constraints.
		/// In cases where the intersection of the two rects does NOT meet the constraints, 
		/// tries to adjust the intersection to find the rect within bounds closest to the original Rect r that meets the constraints.
		/// </summary>
		/// <param name="r">the 'calling' rect</param>
		/// <param name="bounds">The bounds rect into which the final intersection must fit</param>
		/// <param name = "intersect">The out param intersection result</param>
		/// <param name="minWidth">Minimum width constraint for the intersection</param>
		/// <param name="minHeight">Minimum height constraint for the intersection</param>
		/// <returns>
		/// RectConstraintsAdjustment.NONE if no adjustment was necessary, 
		/// RectConstraintsAdjustment.ADJUSTED_TO_MEET_CONSTRAINTS if the intersection was adjusted and now meets the constraints,
		/// RectConstraintsAdjustment.ADJUSTED_BUT_FAILED_TO_MEET_CONSTRAINTS if the intersection was adjusted but still fails to meet constraints (bounds themselves do not meet constraints)
		/// </returns>
		public static RectConstraintsAdjustment ClosestIntersectionThatMeetsConstraints(this Rect r, Rect bounds, out Rect intersect, float minWidth, float minHeight)
		{
			if(!r.Intersects(bounds, out intersect)) {
				// no intersection so return the slice of bounds that is closest to Rect r and that meets min-width/min-height constraints

				intersect = r;

				if(intersect.xMin >= bounds.xMax) {
					intersect.xMin = bounds.xMax - minWidth;
					intersect.xMax = bounds.xMax;
				}
				else if(intersect.xMax <= bounds.xMin) {
					intersect.xMin = bounds.xMin;
					intersect.xMax = bounds.xMin + minWidth;
				}

				if(intersect.yMin >= bounds.yMax) {
					intersect.yMin = bounds.yMax - minHeight;
					intersect.yMax = bounds.yMax;
				}
				else if(r.yMax <= bounds.yMin) {
					intersect.yMin = bounds.yMin;
					intersect.yMax = bounds.yMin + minHeight;
				}

				return RectConstraintsAdjustment.ADJUSTED_TO_MEET_CONSTRAINTS;
			}

			if(intersect.width >= minWidth && intersect.height >= minHeight) {
				return RectConstraintsAdjustment.NONE; 
			}

			if(intersect.width < minWidth) {
				if(Mathf.Approximately(intersect.xMin, bounds.xMin)) {
					intersect.xMax = bounds.xMin + minWidth;
				}
				else if(Mathf.Approximately(intersect.xMax, bounds.xMax)) {
					intersect.xMin = bounds.xMax - minWidth;
				}
				else {
					// intersect is fully within bounds in the x axis, but just too small...
					if(Mathf.Abs(intersect.xMin - bounds.xMin) < Mathf.Abs(bounds.xMax - intersect.xMax)) { 
						intersect.xMin = Mathf.Clamp(intersect.xMax - minWidth, bounds.xMin, bounds.xMax);
						if(intersect.width < minWidth) { // still too small, try setting xMax to bounds.xMax
							intersect.xMax = bounds.xMax;
						}
					}
				}
			}

			if(intersect.height < minHeight) {
				if(Mathf.Approximately(intersect.yMin, bounds.yMin)) {
					intersect.yMax = bounds.yMin + minHeight;
				}
				else if(Mathf.Approximately(intersect.yMax, bounds.yMax)) {
					intersect.yMin = bounds.yMax - minHeight;
				}
				else {
					// intersect is fully within bounds in the y axis, but just too small...
					if(Mathf.Abs(intersect.yMin - bounds.yMin) < Mathf.Abs(bounds.yMax - intersect.yMax)) { 
						intersect.xMin = Mathf.Clamp(intersect.xMax - minWidth, bounds.xMin, bounds.xMax);
						if(intersect.height < minHeight) { // still too small, try setting yMax to bounds.yMax
							intersect.yMax = bounds.yMax;
						}
					}
				}
			}

			return intersect.width >= minWidth && intersect.height >= minHeight? 
				RectConstraintsAdjustment.ADJUSTED_TO_MEET_CONSTRAINTS: RectConstraintsAdjustment.ADJUSTED_BUT_FAILED_TO_MEET_CONSTRAINTS;

		}

	}
}
