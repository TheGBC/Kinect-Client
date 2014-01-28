float4x4 VP;
struct InstancingVSinput
{
 float4 Position : POSITION0;
};
 
struct InstancingVSoutput
{
 float4 Position : POSITION0;
};
 
InstancingVSoutput InstancingVS(InstancingVSinput input,
                                float4 World1 : TEXCOORD0,
                                float4 World2 : TEXCOORD1,
                                float4 World3 : TEXCOORD2,
                                float4 World4 : TEXCOORD3)
{
 InstancingVSoutput output;
 float4 pos = input.Position;
 float4x4 World = float4x4(World1, World2, World3, World4);
 pos = mul(pos, World);
 output.Position = mul(pos, VP);
 return output;
}
 
float4 InstancingPS(InstancingVSoutput input) : COLOR0
{
 return float4(0, 0, 0, 0);
}
 
technique Instancing
{
 pass Pass0
 {
 VertexShader = compile vs_3_0 InstancingVS();
 PixelShader = compile ps_3_0 InstancingPS();
 }
}