import asyncio, json, pathlib, sys
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
async def main():
    p=StdioServerParameters(command=r'C:\Users\User\AppData\Local\Unity\bin\unity.exe',args=['mcp','--project-path',r'C:\Jerry\UnityProject\J2'])
    async with stdio_client(p) as (r,w):
        async with ClientSession(r,w) as s:
            await s.initialize()
            ts=await s.list_tools()
            if len(sys.argv)==1:
                print(json.dumps([{'name':t.name,'schema':t.inputSchema} for t in ts.tools if 'eval' in t.name],indent=2));return
            t=next(t for t in ts.tools if t.name=='eval' or t.name.endswith('_eval'))
            reply=await s.call_tool(t.name,{'code':pathlib.Path(sys.argv[1]).read_text(encoding='utf-8-sig'),'timeout':20000})
            for c in reply.content:
                if hasattr(c,'text'):print(c.text)
            if reply.isError:raise RuntimeError('Unity MCP failed')
asyncio.run(main())
