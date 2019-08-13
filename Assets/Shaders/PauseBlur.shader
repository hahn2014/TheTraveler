Shader "Custom/PauseBlur"{
    //show values to edit in inspector
    Properties{
        [HideInInspector]_MainTex ("Texture", 2D) = "white" {}
        _BlurSize("Blur Size", Range(0,0.1)) = 0
        [KeywordEnum(Low, Medium, High)] _Samples ("Sample amount", Float) = 0
    }

    SubShader{
        // markers that specify that we don't need culling 
        // or reading/writing to the depth buffer
        Cull Off
        ZWrite Off 
        ZTest Always

        Pass{
            CGPROGRAM
            //include useful shader functions
            #include "UnityCG.cginc"

            //define vertex and fragment shader
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _SAMPLES_LOW _SAMPLES_MEDIUM _SAMPLES_HIGH

            #if _SAMPLES_LOW
                #define SAMPLES 10
            #elif _SAMPLES_MEDIUM
                #define SAMPLES 30
            #else
                #define SAMPLES 100
            #endif

            //texture and transforms of the texture
            sampler2D _MainTex;
            float _BlurSize;

            //the object data that's put into the vertex shader
            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            //the data that's used to generate fragments and can be read by the fragment shader
            struct v2f{
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            //the vertex shader
            v2f vert(appdata v){
                v2f o;
                //convert the vertex positions from object space to clip space so they can be rendered
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            //the fragment shader
            fixed4 frag(v2f i) : SV_TARGET{
                //init color variable
                float4 col = 0;
                //iterate over blur samples
                for(float index = 0; index < SAMPLES; index++){
                    //get uv coordinate of sample
                    float2 uv = i.uv + float2(0, (index/(SAMPLES-1) - 0.5) * _BlurSize);
                    //add color at position to color
                    col += tex2D(_MainTex, uv);
                }
                //divide the sum of values by the amount of samples
                col = col / SAMPLES;
                return col;
            }
            ENDCG
        }
    }
}