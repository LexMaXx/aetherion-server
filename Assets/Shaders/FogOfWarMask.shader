Shader "Custom/FogOfWarMask"
{
    Properties
    {
        _PlayerPosition ("Player Position", Vector) = (0, 0, 0, 0)
        _VisibilityRadius ("Visibility Radius", Float) = 60
        _TransitionRange ("Transition Range", Float) = 10
        _FogColor ("Fog Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float4 _PlayerPosition;
            float _VisibilityRadius;
            float _TransitionRange;
            float4 _FogColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Вычисляем расстояние от текущей точки до игрока (только XZ)
                float3 playerPos = _PlayerPosition.xyz;
                float3 worldPos = i.worldPos;

                // Игнорируем высоту Y
                playerPos.y = 0;
                worldPos.y = 0;

                float distance = length(worldPos - playerPos);

                // Вычисляем альфа канал (прозрачность)
                // 0 = прозрачно (видно мир) - внутри радиуса
                // 1 = черный туман (не видно) - за пределами радиуса
                float alpha;

                if (distance < _VisibilityRadius)
                {
                    // Внутри радиуса - полностью прозрачно
                    alpha = 0.0;
                }
                else if (distance < _VisibilityRadius + _TransitionRange)
                {
                    // Переходная зона - плавный градиент
                    float t = (distance - _VisibilityRadius) / _TransitionRange;
                    alpha = smoothstep(0.0, 1.0, t);
                }
                else
                {
                    // За пределами - полностью черный
                    alpha = 1.0;
                }

                // Возвращаем черный цвет с вычисленной прозрачностью
                return float4(_FogColor.rgb, alpha);
            }
            ENDCG
        }
    }
}
