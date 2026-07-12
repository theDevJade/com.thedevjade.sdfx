int vmode = (int)round(_VertexMode);
float3 lpos = v.vertex.xyz;
if (vmode == 0) { lpos.x += sin(_Time.y * _VertexSpeed + lpos.y) * _VertexStrength; }
if (vmode == 1) { lpos += sin(_Time.y * _VertexSpeed + lpos) * _VertexStrength; }
if (vmode == 2) { lpos += sin(_Time.y * _VertexSpeed * 3.0 + lpos.x * 10.0) * _VertexStrength * 0.5; }
if (vmode == 3) { lpos.y += sin(lpos.x * 5.0 + _Time.y * _VertexSpeed) * _VertexStrength; }
if (vmode == 4) { lpos += normalize(lpos) * _VertexStrength; }
if (vmode == 5) { lpos.x += lpos.y * lpos.y * _VertexStrength; }
if (vmode == 6) { float a = _VertexStrength * 10.0; float s = sin(a); float c = cos(a); lpos.xz = float2(c * lpos.x - s * lpos.z, s * lpos.x + c * lpos.z); }
v.vertex.xyz = lpos;
