# Ergonomy — Motion MCP integration paths (2026-11-07)

**Goal:** build/edit reusable animated mascots through ChatGPT, while keeping the Windows app itself privacy-first.

## Actual connection status

- Connected in ChatGPT: Figma, Product Design, Higgsfield, Runway, GitHub (where enabled).
- NOT connected as callable tools in this ChatGPT conversation: Lottie Creator MCP, Rive MCP.
- The plugin directory does not list Lottie Creator or Rive under these names as installable first-party connections as of the research date. **Do not describe either as installed.**
- Custom MCP availability is governed by the ChatGPT plan/workspace; current OpenAI custom MCP documentation says full read/write custom MCP connections are available on Business and Enterprise/Edu. Availability of the Add custom MCP server button must be checked in the user UI.

## 1. Lottie: separate three products

### A. LottieFiles hosted MCP: account/workspace, NOT editor

Official endpoint: `https://mcp.lottiefiles.com/mcp`

- Hosted Streamable HTTP MCP, OAuth login.
- Tools: `operations_list`, `schema_search`, `schema_details`, `graphql_execute`.
- Operates on LottieFiles workspace/projects/animations; does **not** create and edit keyframes in the Lottie Creator browser scene.
- If user has **Plugins → + → Add custom MCP server**, configure URL above, OAuth, approve requested scopes, review tool list, install, invoke in chat. Requires account permission to create custom MCP servers.
- Never paste login tokens into chat.

### B. Lottie Creator WebMCP: editing animation inside supported ChatGPT browser

Official guide: https://docs.lottiefiles.com/en/creator/13_ai-tools/lottie-creator-webmcp

- Official guide demonstrates **ChatGPT desktop app built-in browser** with Lottie Creator.
- Open Creator inside the in-app browser, sign in, open a design.
- In Creator Settings → MCP Settings, check `Enable WebMCP` and browser availability. If it says Temporarily unavailable, cannot force enable.
- Ask assistant to inspect current scene first; test with a single non-destructive change (e.g. selected layer fade-in), then check visual result.
- WebMCP page integration is **not** the same as remote/custom MCP server and may not be supported in all browser/ChatGPT app configurations.

### C. Lottie Creator local MCP: browser bridge

Official guide: https://docs.lottiefiles.com/en/creator/13_ai-tools/lottie-creator-mcp

- Local `npx -y @lottiefiles/creator-mcp@latest`, browser tab open, Settings → MCP Settings → Enable local MCP.
- This is stdio/local; ordinary ChatGPT web chats **cannot** install or attach it directly.
- A legitimate remote relay would require an authorized, secured bridge with controlled write permissions and a persistent host. Do not expose local ports without authentication.

## 2. Rive: official MCP and independent implementations

Official Rive docs: https://rive.app/docs/editor/ai/mcp

- Official MCP exists **inside the Rive desktop Editor (Windows/macOS)**, not as a managed hosted Rive endpoint.
- Official local Streamable HTTP endpoint: `http://127.0.0.1:9791/mcp` while Rive is running; documentation notes Early Access app in connection verification.
- Supports artboards, shapes, state machines, keyframes and other Rive editor operations.
- ChatGPT web cannot connect to localhost. A secure MCP tunnel is needed, plus the user's authorization to create custom MCP servers. Never publicly forward the unauthenticated local endpoint.
- Do **not** treat `npx rive-mcp-server` as Rive's official editor server. That is a distinct independent/source-available project with usage/license conditions: https://github.com/ODU33104/rive-mcp.

## 3. Practical current workflow without custom MCP permissions

1. Create the mascot vector/character system in Figma (editable).
2. Use Higgsfield/Runway for reference motion studies, not assume MP4 output gives an editable rig.
3. Use ChatGPT Desktop Browser + Lottie Creator WebMCP when available to build genuine Lottie scene animations, or manually author format-compatible Lottie data and test it.
4. Use the official Rive editor for advanced rigs and state machines when access becomes available. Rive output must be evaluated for WPF playback/asset conversion before promising in-app integration.
5. Validate motion under reduced-motion settings, contrast, low CPU usage and real Windows overlay focus behavior.

## 4. Acceptance checklist

- [ ] Confirm custom MCP creation entitlement in ChatGPT Plugins interface; don't infer from app installation status.
- [ ] For LottieFiles account MCP: confirm 4 documented tools in a test chat and list a non-sensitive test project.
- [ ] For Lottie Creator WebMCP: inspect a Creator scene and successfully edit only one selected layer.
- [ ] For Rive: confirm secure tunnel and explicit authorization, then inspect one demo artboard before allowing writes.
- [ ] Verify exports (Lottie JSON, dotLottie, Rive .riv or spritesheet) separately; no file format is assumed WPF-compatible without testing.
- [ ] Check screen-reader accommodations, reduced motion, dark/light contrast, multi-monitor and actual overlay responsiveness.

## References

- OpenAI custom MCP: https://help.openai.com/en/articles/12584461-connecting-custom-mcp-servers-in-chatgpt
- Lottie platform MCP: https://docs.lottiefiles.com/en/platform/mcp
- Lottie Creator local MCP: https://docs.lottiefiles.com/en/creator/13_ai-tools/lottie-creator-mcp
- Lottie Creator WebMCP: https://docs.lottiefiles.com/en/creator/13_ai-tools/lottie-creator-webmcp
- Official Rive MCP: https://rive.app/docs/editor/ai/mcp
- Rive alternative: https://github.com/ODU33104/rive-mcp

**No deployment, authentication, or tool connection is implied by this document.**
