using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Blazor.Diagrams.Core.PathGenerators;
using SvgPathProperties;

namespace NodeDev.Blazor.DiagramsModels;


public class SmoothPathGeneratorWithDirectVertices : PathGenerator
{
	private readonly double _margin;

	public SmoothPathGeneratorWithDirectVertices(double margin = 125)
	{
		_margin = margin;
	}

	public override PathGeneratorResult GetResult(Diagram diagram, BaseLinkModel link, Point[] route, Point source, Point target)
	{
		route = ConcatRouteAndSourceAndTarget(route, source, target);

		if (route.Length > 2)
			return CurveThroughPoints(route, link);

		route = GetRouteWithCurvePoints(link, route[0], route[1], true, true);
		double? sourceAngle = null;
		double? targetAngle = null;

		if (link.SourceMarker != null)
		{
			sourceAngle = AdjustRouteForSourceMarker(route, link.SourceMarker.Width);
		}

		if (link.TargetMarker != null)
		{
			targetAngle = AdjustRouteForTargetMarker(route, link.TargetMarker.Width);
		}

		var path = new SvgPath()
			.AddMoveTo(route[0].X, route[0].Y)
			.AddCubicBezierCurve(route[1].X, route[1].Y, route[2].X, route[2].Y, route[3].X, route[3].Y);

		return new PathGeneratorResult(path, Array.Empty<SvgPath>(), sourceAngle, route[0], targetAngle, route[^1]);
	}

	private PathGeneratorResult CurveThroughPoints(Point[] route, BaseLinkModel link)
	{
		double? sourceAngle = null;
		double? targetAngle = null;

		if (link.SourceMarker != null)
		{
			sourceAngle = AdjustRouteForSourceMarker(route, link.SourceMarker.Width);
		}

		if (link.TargetMarker != null)
		{
			targetAngle = AdjustRouteForTargetMarker(route, link.TargetMarker.Width);
		}

		var paths = new SvgPath[route.Length - 1];
		var fullPath = new SvgPath().AddMoveTo(route[0].X, route[0].Y);

		for (var i = 0; i < route.Length - 1; i++)
		{
			var localRoute = GetRouteWithCurvePoints(link, route[i], route[i + 1], i == 0, i + 2 == route.Length);

			fullPath.AddCubicBezierCurve(localRoute[1].X, localRoute[1].Y, localRoute[2].X, localRoute[2].Y, localRoute[3].X, localRoute[3].Y);
			paths[i] = new SvgPath().AddMoveTo(route[i].X, route[i].Y).AddCubicBezierCurve(localRoute[1].X, localRoute[1].Y, localRoute[2].X, localRoute[2].Y, localRoute[3].X, localRoute[3].Y);
		}

		// Todo: adjust marker positions based on closest control points
		return new PathGeneratorResult(fullPath, paths, sourceAngle, route[0], targetAngle, route[^1]);
	}

	private Point[] GetRouteWithCurvePoints(BaseLinkModel link, Point p0, Point p1, bool isFirstInRoute, bool isLastInRoute)
	{
		var cX = (p0.X + p1.X) / 2;
		var cY = (p0.Y + p1.Y) / 2;
		var curvePointA = GetCurvePoint(link.Source, p0.X, p0.Y, cX, cY, first: true, isFirstInRoute, isLastInRoute);
		var curvePointB = GetCurvePoint(link.Target, p1.X, p1.Y, cX, cY, first: false, isFirstInRoute, isLastInRoute);
		return [p0, curvePointA, curvePointB, p1];
	}

	private Point GetCurvePoint(Anchor anchor, double pX, double pY, double cX, double cY, bool first, bool isFirstInRoute, bool isLastInRoute)
	{
		if (anchor is PositionAnchor)
			return new Point(cX, cY);

		if (anchor is SinglePortAnchor spa)
		{
			PortAlignment portAlignment;
			if ((isFirstInRoute && first) || (isLastInRoute && !first))
				portAlignment = spa.Port.Alignment;
			else if (pX < cX)
				portAlignment = PortAlignment.Right;
			else
				portAlignment = PortAlignment.Left;

			return GetCurvePoint(pX, pY, cX, cY, portAlignment);
		}
		else if (anchor is ShapeIntersectionAnchor or DynamicAnchor or LinkAnchor)
		{
			if (Math.Abs(pX - cX) >= Math.Abs(pY - cY))
			{
				return first ? new Point(cX, pY) : new Point(cX, cY);
			}
			else
			{
				return first ? new Point(pX, cY) : new Point(cX, cY);
			}
		}
		else
		{
			throw new DiagramsException($"Unhandled Anchor type {anchor.GetType().Name} when trying to find curve point");
		}
	}

	private Point GetCurvePoint(double pX, double pY, double cX, double cY, PortAlignment? alignment)
	{
		var margin = Math.Min(_margin, Math.Pow(Math.Pow(pX - cX, 2) + Math.Pow(pY - cY, 2), .5));
		return alignment switch
		{
			PortAlignment.Top => new Point(pX, Math.Min(pY - margin, cY)),
			PortAlignment.Bottom => new Point(pX, Math.Max(pY + margin, cY)),
			PortAlignment.TopRight => new Point(Math.Max(pX + margin, cX), Math.Min(pY - margin, cY)),
			PortAlignment.BottomRight => new Point(Math.Max(pX + margin, cX), Math.Max(pY + margin, cY)),
			PortAlignment.Right => new Point(Math.Max(pX + margin, cX), pY),
			PortAlignment.Left => new Point(Math.Min(pX - margin, cX), pY),
			PortAlignment.BottomLeft => new Point(Math.Min(pX - margin, cX), Math.Max(pY + margin, cY)),
			PortAlignment.TopLeft => new Point(Math.Min(pX - margin, cX), Math.Min(pY - margin, cY)),
			_ => new Point(cX, cY),
		};
	}
}