Shader "NSMB/UnlitEyesFlipbook"
{
    Properties
    {
        _BaseMap ("Eye Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5

        // Eye sheet layout: (cols, rows).
        // Unity 6 imports `mario_eyes.png` as a 2D-array with flipbook columns=4, rows=1.
        // In Unity 2017 we emulate that by treating the source PNG as a 4x1 sheet.
        _EyeGrid ("Eye Grid", Vector) = (4,1,0,0)
        _EyeState ("Eye State", Float) = 0
        _UseUv2 ("Use UV2", Float) = 0
        _RowFromTop ("Row From Top", Float) = 1

        // Compatibility aliases.
        _MainTex ("Main Tex", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        LOD 100

        // Eyes are a plane; keep double-sided and never depth-occluded by the head.
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            fixed4 _BaseColor;
            fixed _Cutoff;

            float4 _EyeGrid;   // x=cols, y=rows
            float _EyeState;
            float _UseUv2;
            float _RowFromTop;

            // Aliases
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv0 = v.uv0;
                o.uv1 = v.uv1;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float cols = max(1.0, _EyeGrid.x);
                float rows = max(1.0, _EyeGrid.y);
                float maxIndex = (cols * rows) - 1.0;
                float idxf = clamp(floor(_EyeState + 0.5), 0.0, maxIndex);

                float2 cell = float2(1.0 / cols, 1.0 / rows);
                float x = fmod(idxf, cols);
                float y = floor(idxf / cols);

                float2 rawUv = (_UseUv2 > 0.5) ? i.uv1 : i.uv0;
                // Some FBX exports pack left/right eyes side-by-side by offsetting one eye's UVs beyond 1.0 (e.g. 1..2).
                // - `frac()` wraps into 0..1 so sampling stays stable (avoids seams/cutoffs).
                // - If the UVs live in an "odd" unit tile (floor(U)=1,3,...) then mirror X so the opposite eye
                //   doesn't look like a copy-paste of the first one.
                float uTile = floor(rawUv.x);
                float flipX = fmod(abs(uTile), 2.0);

                float2 baseUv = frac(rawUv);
                if (flipX > 0.5) {
                    baseUv.x = 1.0 - baseUv.x;
                }

                // `mario_eyes.png` is authored in image space where "row 0" is the top row. In Unity UV space,
                // V=0 corresponds to the bottom of the texture. Default `_RowFromTop=1` selects rows from the
                // top by flipping the row index.
                float yFromTop = (rows - 1.0 - y);
                float row = lerp(y, yFromTop, saturate(_RowFromTop));

                float2 uv = baseUv * cell + float2(x * cell.x, row * cell.y);
                uv = TRANSFORM_TEX(uv, _BaseMap);

                fixed4 c = tex2D(_BaseMap, uv) * _BaseColor;
                c *= _Color;
                clip(c.a - _Cutoff);
                c.a = 1;
                return c;
            }
            ENDCG
        }
    }

    Fallback "Transparent/Cutout/Diffuse"
}
