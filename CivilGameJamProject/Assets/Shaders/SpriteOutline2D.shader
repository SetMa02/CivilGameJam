Shader "Custom/2D/SpriteOutline2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1, 0.9, 0.2, 1)
        _OutlineThickness ("Outline Thickness", Range(0, 15)) = 2.0
        [MaterialToggle] _OutlineEnabled ("Outline Enabled", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineThickness;
            float _OutlineEnabled;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, IN.texcoord) * IN.color;

                if (_OutlineEnabled > 0.5 && _OutlineThickness > 0.0)
                {
                    // Sample alpha around current pixel
                    float2 px = _MainTex_TexelSize.xy * _OutlineThickness;
                    float aUp    = tex2D(_MainTex, IN.texcoord + float2(0, px.y)).a;
                    float aDown  = tex2D(_MainTex, IN.texcoord - float2(0, px.y)).a;
                    float aRight = tex2D(_MainTex, IN.texcoord + float2(px.x, 0)).a;
                    float aLeft  = tex2D(_MainTex, IN.texcoord - float2(px.x, 0)).a;

                    float aUpRight   = tex2D(_MainTex, IN.texcoord + float2(px.x, px.y) * 0.7071).a;
                    float aUpLeft    = tex2D(_MainTex, IN.texcoord + float2(-px.x, px.y) * 0.7071).a;
                    float aDownRight = tex2D(_MainTex, IN.texcoord + float2(px.x, -px.y) * 0.7071).a;
                    float aDownLeft  = tex2D(_MainTex, IN.texcoord + float2(-px.x, -px.y) * 0.7071).a;

                    float maxNeighborAlpha = max(max(max(aUp, aDown), max(aRight, aLeft)),
                                                 max(max(aUpRight, aUpLeft), max(aDownRight, aDownLeft)));

                    // If current pixel is transparent or semi-transparent, but neighbor is solid -> outline
                    if (col.a < 0.95 && maxNeighborAlpha > 0.1)
                    {
                        float outlineAlpha = maxNeighborAlpha * (1.0 - col.a) * _OutlineColor.a;
                        fixed4 outline = fixed4(_OutlineColor.rgb, outlineAlpha);
                        col = lerp(outline, col, col.a);
                        col.a = max(col.a, outlineAlpha);
                    }
                }

                return col;
            }
            ENDCG
        }
    }
}
