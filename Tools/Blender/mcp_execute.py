"""Send a UTF-8 Python file through the user's installed Blender MCP server."""
import asyncio
import json
import os
import pathlib
import sys
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

async def main():
    code = pathlib.Path(sys.argv[1]).read_text(encoding="utf-8-sig")
    params = StdioServerParameters(
        command=sys.executable, args=["-m", "blmcp"],
        env={**os.environ, "BLENDER_MCP_HOST": "127.0.0.1", "BLENDER_MCP_PORT": "9876"})
    async with stdio_client(params) as (reader, writer):
        async with ClientSession(reader, writer) as session:
            await session.initialize()
            reply = await session.call_tool("execute_blender_code", {"code": code})
            data = reply.structuredContent
            if data is None:
                data = json.loads(reply.content[0].text)
            print(json.dumps(data, ensure_ascii=True, indent=2))
            if reply.isError or data.get("status") == "error":
                raise RuntimeError("Blender MCP execution failed")

if __name__ == "__main__":
    asyncio.run(main())
