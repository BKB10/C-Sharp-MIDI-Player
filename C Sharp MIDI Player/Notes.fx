float Spin : VertexSpin;

struct VS_IN
{
    float2 pos : POSITION;
    float4 col : COLOR;
};

struct PS_IN
{
    float4 pos : SV_POSITION;
    float4 col : COLOR;
};

PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN)0;

    output.pos = float4(input.pos.x, input.pos.y, 0, 1);

    output.pos.x = cos(Spin) * input.pos.x - sin(Spin) * input.pos.y;
    output.pos.y = sin(Spin) * input.pos.x + cos(Spin) * input.pos.y;
    output.col = input.col;

    return output;
}

float4 PS(PS_IN input) : SV_Target
{
    return input.col;
}