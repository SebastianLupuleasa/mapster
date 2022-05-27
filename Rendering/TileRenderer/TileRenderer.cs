using Mapster.Common.MemoryMappedTypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Mapster.Rendering;
public enum myFeatures
{
    highway,
    water,
    boundary,
    population,
    place,
    natural,
    name,
    building,
    leisure,
    amenity
}


static class StuffMethods
{

    public static BaseShape? doLogic(this myFeatures s1, MapFeatureData feature)
    {
        switch (s1)
        {
            case myFeatures.highway:
                return new Road(feature.Coordinates, feature.Type == GeometryType.Point);

            case myFeatures.water:
                return new Waterway(feature.Coordinates, feature.Type == GeometryType.Polygon);

            case myFeatures.boundary:
                return new Border(feature.Coordinates);
            
            case myFeatures.population:
                return new PopulatedPlace(feature.Coordinates, feature);

            case myFeatures.place:
                return new PopulatedPlace(feature.Coordinates, feature);

            case myFeatures.natural:
                if (feature.Type == GeometryType.Polygon)
                    return new GeoFeature(feature.Coordinates, GeoFeature.GeoFeatureType.Plain);
                else
                    return null;
            
            case myFeatures.building:
                if (feature.Type == GeometryType.Polygon)
                    return new GeoFeature(feature.Coordinates, GeoFeature.GeoFeatureType.Residential);
                else
                    return null;

            case myFeatures.leisure:
                if (feature.Type == GeometryType.Polygon)
                    return new GeoFeature(feature.Coordinates, GeoFeature.GeoFeatureType.Residential);
                else
                    return null;

            case myFeatures.amenity:
                if (feature.Type == GeometryType.Polygon)
                    return new GeoFeature(feature.Coordinates, GeoFeature.GeoFeatureType.Residential);
                else
                    return null;

            default:
              return null;

        }
    }
}

public static class TileRenderer
{
    public static BaseShape Tessellate(this MapFeatureData feature, ref BoundingBox boundingBox, ref PriorityQueue<BaseShape, int> shapes)
    {
        BaseShape? baseShape = null;
        var featureType = feature.Type;
        foreach (KeyValuePair<string, string> entry in feature.Properties)
        {
            // do something with entry.Value or entry.Key
        try
            {
                myFeatures featureTyped = (myFeatures)Enum.Parse(typeof(myFeatures), entry.Key);
                baseShape = featureTyped.doLogic(feature);
                if (baseShape != null)
                {
                    shapes.Enqueue(baseShape, baseShape.ZIndex);
                }
            }
            catch (Exception e) {}
            baseShape = null;
        }
    

     if (feature.Properties.Any(p => p.Key.StartsWith("boundary") && p.Value.StartsWith("forest")))
        {
            var coordinates = feature.Coordinates;
            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Forest);
            baseShape = geoFeature;
            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
        }
     else if (feature.Properties.Any(p => p.Key.StartsWith("landuse") && (p.Value.StartsWith("forest") || p.Value.StartsWith("orchard"))))
        {
            var coordinates = feature.Coordinates;
            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Forest);
            baseShape = geoFeature;
            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
        }
     
        

        if (baseShape != null)
        {
            for (var j = 0; j < baseShape.ScreenCoordinates.Length; ++j)
            {
                boundingBox.MinX = Math.Min(boundingBox.MinX, baseShape.ScreenCoordinates[j].X);
                boundingBox.MaxX = Math.Max(boundingBox.MaxX, baseShape.ScreenCoordinates[j].X);
                boundingBox.MinY = Math.Min(boundingBox.MinY, baseShape.ScreenCoordinates[j].Y);
                boundingBox.MaxY = Math.Max(boundingBox.MaxY, baseShape.ScreenCoordinates[j].Y);
            }
        }
        return baseShape;
    }

    public static Image<Rgba32> Render(this PriorityQueue<BaseShape, int> shapes, BoundingBox boundingBox, int width, int height)
    {
        var canvas = new Image<Rgba32>(width, height);

        // Calculate the scale for each pixel, essentially applying a normalization
        var scaleX = canvas.Width / (boundingBox.MaxX - boundingBox.MinX);
        var scaleY = canvas.Height / (boundingBox.MaxY - boundingBox.MinY);
        var scale = Math.Min(scaleX, scaleY);

        // Background Fill
        canvas.Mutate(x => x.Fill(Color.White));
        while (shapes.Count > 0)
        {
            var entry = shapes.Dequeue();
            entry.TranslateAndScale(boundingBox.MinX, boundingBox.MinY, scale, canvas.Height);
            canvas.Mutate(x => entry.Render(x));
        }

        return canvas;
    }

    public struct BoundingBox
    {
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;
    }
}