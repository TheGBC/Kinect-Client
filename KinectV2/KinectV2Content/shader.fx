float3 DiffuseDirection = float3(0, 0, 1);
float4 DiffuseColor = float4(1, 1, 1, 1);
float4x4 View;
float4x4 Projection;
float4x4 Rotation;
float4x4 PostWorld;
float4x4 PostWorldInverse;

struct InstancingVSinput
{
 float4 Position : POSITION0;
 float4 Color: COLOR0;
 float3 Normal: NORMAL0;
};
 
struct InstancingVSoutput
{
 float4 Position : POSITION0;
 float4 Color: COLOR0;
 float4 Normal: NORMAL0;
};
 
InstancingVSoutput InstancingVS(InstancingVSinput input,
                                float4 PreWorld1 : TEXCOORD0,
                                float4 PreWorld2 : TEXCOORD1,
                                float4 PreWorld3 : TEXCOORD2, 
                                float4 PreWorld4 : TEXCOORD3,
                                float4 PreWorldI1 : TEXCOORD4,
                                float4 PreWorldI2 : TEXCOORD5,
                                float4 PreWorldI3 : TEXCOORD6, 
                                float4 PreWorldI4 : TEXCOORD7,
                                float4 color : COLOR1)
{
 InstancingVSoutput output;
 float4x4 PreWorld = float4x4(PreWorld1, PreWorld2, PreWorld3, PreWorld4);
 float4x4 PreWorldInverse = float4x4(PreWorldI1, PreWorldI2, PreWorldI3, PreWorldI4);

 float4x4 World = mul(PreWorld, Rotation);
 World = mul(World, PostWorld);

 float4x4 WorldInverse = mul(PostWorldInverse, transpose(Rotation));
 WorldInverse = mul(WorldInverse, PreWorldInverse);

 float4 pos = input.Position;
 pos = mul(pos, World);
 pos = mul(pos, View);
 pos = mul(pos, Projection);
 output.Position = pos;
 output.Color = color;
 float4 norm4 = float4(input.Normal, 1);
 output.Normal = normalize(mul(norm4, transpose(WorldInverse)));
 return output;
}
 
float4 InstancingPS(InstancingVSoutput input) : COLOR0
{
 float4 norm = input.Normal;
 float4 diffuse = saturate(DiffuseColor * dot(norm, DiffuseDirection));
 return saturate(.8 * input.Color + .3 * diffuse);
}
 
technique Instancing
{
 pass Pass0
 {
 VertexShader = compile vs_3_0 InstancingVS();
 PixelShader = compile ps_3_0 InstancingPS();
 }
}