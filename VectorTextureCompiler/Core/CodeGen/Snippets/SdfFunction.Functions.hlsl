float SdfxSdfRect(float2 p, float2 b)
{
    float2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

float SdfxSdfCircle(float2 p, float r)
{
    return length(p) - r;
}

float SdfxSdfRoundRect(float2 p, float2 b, float r)
{
    float2 q = abs(p) - b + r;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

float SdfxSdfCapsule(float2 p, float2 a, float2 b, float r)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = clamp(dot(pa, ba) / max(dot(ba, ba), 1e-6), 0.0, 1.0);
    return length(pa - ba * h) - r;
}

float SdfxSdfRing(float2 p, float r, float w)
{
    return abs(length(p) - r) - w;
}

float SdfxSdfStar(float2 p, float r, float n, float m)
{
    float fn = max(n, 1.0);
    float an = 3.141593 / fn;
    float en = 3.141593 / m;
    float2 acs = float2(cos(an), sin(an));
    float bn = fmod(atan2(p.x, p.y) + 2.0 * an, 2.0 * an) - an;
    p = float2(length(p) * cos(bn), length(p) * sin(bn));
    p -= r * acs;
    p += acs * clamp(-dot(p, acs), 0.0, r * acs.y / acs.x);
    return length(p) * sign(p.x);
}

float SdfxSdfSegment(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = clamp(dot(pa, ba) / max(dot(ba, ba), 1e-6), 0.0, 1.0);
    return length(pa - ba * h);
}

float SdfxSdfPathFill(int start, int count, float2 uv)
{
    float dSq = 1e10;
    float s = 1.0;

    [loop]
    for (int e = 0; e < count; e++)
    {
        float4 seg = SdfxFetchPathEdge(start + e);
        float2 a = seg.xy;
        float2 b = seg.zw;

        float2 ed = b - a;
        float2 w  = uv - a;
        float2 q  = w - ed * clamp(dot(w, ed) / max(dot(ed, ed), 1e-12), 0.0, 1.0);
        dSq = min(dSq, dot(q, q));

        bool3 c = bool3(uv.y >= a.y, uv.y < b.y, ed.x * w.y > ed.y * w.x);
        if (all(c) || !any(c)) { s = -s; }
    }

    return s * sqrt(dSq);
}

float SdfxSdfPathStroke(int start, int count, float2 uv, float radius)
{
    float dSq = 1e10;

    [loop]
    for (int e = 0; e < count; e++)
    {
        float4 seg = SdfxFetchPathEdge(start + e);
        float2 ed = seg.zw - seg.xy;
        float2 w  = uv - seg.xy;
        float2 q  = w - ed * clamp(dot(w, ed) / max(dot(ed, ed), 1e-12), 0.0, 1.0);
        dSq = min(dSq, dot(q, q));
    }

    return sqrt(dSq) - radius;
}

float SdfxEvalPrimitive(SdfxPrimitive p, float2 uv)
{
    if (p.type == SDFX_TYPE_POLYGON && p.pathCount > 0)
    {
        return SdfxSdfPathFill(p.pathStart, p.pathCount, uv);
    }

    if (p.type == SDFX_TYPE_POLYLINE)
    {
        return SdfxSdfPathStroke(p.pathStart, p.pathCount, uv, p.strokeRadius);
    }

    float2 local = uv - (p.pos + p.size * 0.5);
    float  rc = cos(-p.rotation);
    float  rs = sin(-p.rotation);
    local = float2(rc * local.x - rs * local.y, rs * local.x + rc * local.y);
    float2 halfExt = p.size * 0.5;

    float d;
    if (p.type == SDFX_TYPE_CIRCLE)
    {
        d = SdfxSdfCircle(local, min(halfExt.x, halfExt.y));
    }
    else if (p.type == SDFX_TYPE_LINE || p.type == SDFX_TYPE_BEZIER)
    {
        float lineRadius = halfExt.x;
        float lineHalfLen = max(halfExt.y - lineRadius, 0.0);
        d = SdfxSdfSegment(local, float2(0.0, -lineHalfLen), float2(0.0, lineHalfLen)) - lineRadius;
    }
    else if (p.type == SDFX_TYPE_ROUNDRECT)
    {
        float corner = min(halfExt.x, halfExt.y) * 0.25;
        d = SdfxSdfRoundRect(local, halfExt, corner);
    }
    else if (p.type == SDFX_TYPE_CAPSULE)
    {
        float r = halfExt.x;
        float h = max(halfExt.y - r, 0.0);
        d = SdfxSdfCapsule(local, float2(0.0, -h), float2(0.0, h), r);
    }
    else if (p.type == SDFX_TYPE_RING)
    {
        d = SdfxSdfRing(local, min(halfExt.x, halfExt.y), halfExt.x * 0.25);
    }
    else if (p.type == SDFX_TYPE_STAR)
    {
        d = SdfxSdfStar(local, min(halfExt.x, halfExt.y), 5.0, 2.5);
    }
    else if (p.type == SDFX_TYPE_ARC)
    {
        d = SdfxSdfRing(local, min(halfExt.x, halfExt.y), halfExt.x * 0.15);
    }
    else if (p.type == SDFX_TYPE_ELLIPSE)
    {
        float2 r = max(halfExt, 1e-4);
        float k = length(local / r);
        d = (k - 1.0) * min(r.x, r.y);
    }
    else if (p.type == SDFX_TYPE_TRIANGLE)
    {
        float2 q = abs(local);
        d = max(q.x * 0.866025 + local.y * 0.5, -local.y) - halfExt.y * 0.5;
    }
    else if (p.type == SDFX_TYPE_HEXAGON)
    {
        float2 q = abs(local);
        d = max(q.x * 0.866025 + local.y * 0.5, q.x) - halfExt.x;
    }
    else if (p.type == SDFX_TYPE_CROSS)
    {
        float2 a = halfExt * 0.35;
        d = min(SdfxSdfRect(local, float2(halfExt.x, a.y)), SdfxSdfRect(local, float2(a.x, halfExt.y)));
    }
    else if (p.type == SDFX_TYPE_HEART)
    {
        local.y += halfExt.y * 0.25;
        local *= 1.2;
        d = SdfxSdfCircle(local - float2(0.0, 0.25 * halfExt.y), halfExt.x * 0.45);
    }
    else if (p.type == SDFX_TYPE_DONUT)
    {
        d = SdfxSdfRing(local, min(halfExt.x, halfExt.y) * 0.65, halfExt.x * 0.2);
    }
    else
    {
        d = SdfxSdfRect(local, halfExt);
    }

    return d;
}
