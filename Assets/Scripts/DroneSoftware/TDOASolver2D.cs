using System;
using UnityEngine;
using System.Collections.Generic;

public static class TDOASolver2D {
    // Solve the 2D multilateration (3 receivers) TDOA problem.
    // positions: array of length 3 with Vector2 receiver coordinates
    // times: array of length 3 with receive timestamps
    // c: propagation speed (e.g., 343 m/s for sound)
    //
    // Returns 0, 1, or 2 possible source locations.
    public static List<(Vector2 position, float emissionTime)> Solve(
        Vector2[] positions,
        float[] times,
        float c = 343f) {
        if (positions.Length != 3 || times.Length != 3)
            throw new ArgumentException("Need exactly 3 positions and 3 times.");

        Vector2 r1 = positions[0];
        Vector2 r2 = positions[1];
        Vector2 r3 = positions[2];

        float t1 = times[0];
        float t2 = times[1];
        float t3 = times[2];

        // Build M = [r2-r1; r3-r1]
        Vector2 m1 = r2 - r1;
        Vector2 m2 = r3 - r1;

        float det = m1.x * m2.y - m1.y * m2.x;
        if (Mathf.Abs(det) < 1e-9f)
            throw new Exception("Receiver geometry is degenerate (collinear).");

        float invDet = 1f / det;

        // delta times
        float dt2 = t2 - t1;
        float dt3 = t3 - t1;

        // v = c^2 * [dt2, dt3]
        Vector2 v = new Vector2(c * c * dt2, c * c * dt3);

        // b vector
        float RHS(Vector2 ri, float ti) {
            float ri2 = Vector2.Dot(ri, ri);
            float r12 = Vector2.Dot(r1, r1);
            return -0.5f * (c * c * (ti * ti - t1 * t1) - (ri2 - r12));
        }

        Vector2 b = new Vector2(RHS(r2, t2), RHS(r3, t3));

        // Compute p = M^{-1} v, q = M^{-1} b
        Vector2 Minv(Vector2 col)  // multiply inverse(M) by a column vector
        {
            float x = (col.x * m2.y - col.y * m2.x) * invDet;
            float y = (-col.x * m1.y + col.y * m1.x) * invDet;
            return new Vector2(x, y);
        }

        Vector2 p = Minv(v);
        Vector2 q = Minv(b);

        // Now form the quadratic in t0
        Vector2 qMinusR1 = q - r1;

        float A = Vector2.Dot(p, p);
        float B = 2f * Vector2.Dot(p, qMinusR1);
        float C = Vector2.Dot(qMinusR1, qMinusR1);

        // Right side: c^2 (t1 - t0)^2
        // Move to left: (A - c^2) t0^2 + (B + 2 c^2 t1) t0 + (C - c^2 t1^2) = 0

        float aa = A - c * c;
        float bb = B + 2f * c * c * t1;
        float cc = C - c * c * t1 * t1;

        List<(Vector2, float)> sol = new List<(Vector2, float)>();

        // Handle near-linear case
        if (Mathf.Abs(aa) < 1e-9f) {
            if (Mathf.Abs(bb) < 1e-9f)
                return sol; // no unique solution

            float t0 = -cc / bb;
            Vector2 pos = p * t0 + q;
            sol.Add((pos, t0));
            return sol;
        }

        float disc = bb * bb - 4f * aa * cc;
        if (disc < -1e-9f)
            return sol; // no real solutions

        disc = Mathf.Max(0f, disc);
        float sqrtD = Mathf.Sqrt(disc);

        float t0a = (-bb + sqrtD) / (2f * aa);
        float t0b = (-bb - sqrtD) / (2f * aa);

        Vector2 solA = p * t0a + q;
        Vector2 solB = p * t0b + q;

        sol.Add((solA, t0a));
        if (disc > 1e-12f)  // two different roots
            sol.Add((solB, t0b));

        return sol;
    }
}
