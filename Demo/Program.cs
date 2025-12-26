using Geometry;
using Demo;
using WorldModel = global::World.World;
using Sphere = global::World.Sphere;
using Cylinder = global::World.Cylinder;

internal class Program
{
    private static void Main()
    {
        var world = new WorldModel();

        // Clean solar system: sun + 8 planets, each at a different orbital phase,
        // with moons in distinct orbits (no asteroids, no extra geometry).

        var sunCenter = new Point(0, 0, 0);
        world.Add(new Sphere(radius: 180, center: sunCenter));

        // Demo tuning: very thin orbit rings
        const int RingThickness = 2;
        const int RingHeight    = 2;

        // Math helpers kept local so the demo remains self-contained and readable
        static (double x, double y, double z) RotateXZ(double x, double y, double z, double xDeg, double zDeg)
        {
            double rx = xDeg * Math.PI / 180.0;
            double rz = zDeg * Math.PI / 180.0;
            double cx = Math.Cos(rx), sx = Math.Sin(rx);
            double cz = Math.Cos(rz), sz = Math.Sin(rz);
            double y1 = y * cx - z * sx;
            double z1 = y * sx + z * cx;
            double x1 = x;
            double x2 = x1 * cz - y1 * sz;
            double y2 = x1 * sz + y1 * cz;
            return (x2, y2, z1);
        }

        // World helpers to keep the orchestration terse
        void AddOrbitRing(Point center, long radius, double incDeg, double ascDeg)
            => world.Add(new Cylinder(radius: radius, thickness: RingThickness, height: RingHeight,
                                              center: center, segments: null,
                                              xTiltDeg: incDeg, yTiltDeg: 0, zSpinDeg: ascDeg));

        static Point OffsetOnTiltedCircle(Point center, long radius, double phaseDeg, double incDeg, double ascDeg)
        {
            double ph = phaseDeg * Math.PI / 180.0;
            var v = RotateXZ(Math.Cos(ph) * radius, Math.Sin(ph) * radius, 0.0, incDeg, ascDeg);
            return new Point(center.X + (long)Math.Round(v.x, MidpointRounding.AwayFromZero),
                             center.Y + (long)Math.Round(v.y, MidpointRounding.AwayFromZero),
                             center.Z + (long)Math.Round(v.z, MidpointRounding.AwayFromZero));
        }

        void AddPlanet(in Planets.Planet p, double phaseDeg)
        {
            // Orbit ring + body on its tilted plane
            AddOrbitRing(sunCenter, p.OrbitRadius, p.InclinationDeg, p.AscendingNodeDeg);
            var pc = OffsetOnTiltedCircle(sunCenter, p.OrbitRadius, phaseDeg, p.InclinationDeg, p.AscendingNodeDeg);
            world.Add(new Sphere(p.Radius, pc));

            // Moons (each with a subtle additional inclination)
            for (int i = 0; i < p.MoonOrbits.Length; i++)
            {
                double moonAsc = p.AscendingNodeDeg + i * (360.0 / (p.MoonOrbits.Length * 3.0));
                double moonInc = p.InclinationDeg + p.MoonExtraInclineDeg;
                AddOrbitRing(pc, p.MoonOrbits[i], moonInc, moonAsc);

                var mp = OffsetOnTiltedCircle(pc, p.MoonOrbits[i], phaseDeg * 2.0 + i * (360.0 / p.MoonOrbits.Length),
                                               moonInc, moonAsc);
                world.Add(new Sphere(p.MoonRadius, mp));
            }
        }

        // Mercury, Venus, Earth(+moon), Mars(+2), Jupiter(+4), Saturn(+3), Uranus(+2), Neptune(+1)
        AddPlanet(Planets.Mercury, phaseDeg: 10);
        AddPlanet(Planets.Venus, phaseDeg: 60);
        AddPlanet(Planets.Earth, phaseDeg: 130);
        AddPlanet(Planets.Mars, phaseDeg: 210);
        AddPlanet(Planets.Jupiter, phaseDeg: 280);
        AddPlanet(Planets.Saturn, phaseDeg: 330);
        AddPlanet(Planets.Uranus, phaseDeg: 45);
        AddPlanet(Planets.Neptune, phaseDeg: 95);

        world.Save("clean_system.stl");
    }
}
