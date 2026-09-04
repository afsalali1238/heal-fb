/// Page assembly: content + specs + figures -> static HTML documents.
///
/// This module is the seam the static-site generator calls (see
/// scripts/render.mjs): `areaRoutes` lists the routes, `renderHome` and
/// `renderArea` return plain strings. No framework, no runtime — patients
/// get static files with nothing to download beyond them.
module Physio.App

open Physio.Domain
open Physio.Content
open Physio.Safety
open Physio.Figures
open Physio.Specs
open Physio.Checks

let private esc (s : string) : string =
    s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")

/// Attribute escaping: element escaping plus double quotes.
let private attrEsc (s : string) : string = (esc s).Replace("\"", "&quot;")

/// Build-time gate, re-exported so the render script fails the build on
/// any figure problem before writing a single file.
let validateAll () : GateReport = Checks.validateAll ()

/// All area routes the generator must write.
let areaRoutes () : string list = areas |> List.map (fun a -> a.Id)

let private figureFor (id : string) : Figure * Arrow =
    match specFor id with
    | None -> failwith $"No figure spec for item \"{id}\". Run the gate first."
    | Some spec ->
        let fig = buildFigure spec.End
        fig, arrowFor spec

/// Area thumbnail: the first item's end-range figure, drawn from the same
/// geometry as every other picture, so it can never disagree with them.
let private thumbnailFor (areaId : string) : string =
    match itemsForArea areaId with
    | [] -> ""
    | first :: _ ->
        let fig, _ = figureFor first.Id
        let art = figureSvg fig None $"{first.AreaId} area illustration"
        $"""<span class="thumb" aria-hidden="true">{art}</span>"""

let private optRow (label : string) (fmt : int -> string) (value : int option) : string =
    match value with
    | Some v -> $"""<div class="d"><dt>{label}</dt><dd>{fmt v}</dd></div>"""
    | None -> ""

let private itemCard (item : Item) : string =
    let fig, arrow = figureFor item.Id
    let art = figureSvg fig (Some arrow) item.ImageAlt
    let sectionName =
        match item.Section with
        | Stretching -> "Stretch"
        | Exercise -> "Exercise"
    let dose =
        optRow "Hold" (fun s -> $"{s} sec") item.Dose.HoldSeconds
        + optRow "Repeat" (fun r -> $"{r} times") item.Dose.Reps
        + optRow "Sets" string item.Dose.Sets
        + (if item.Dose.EachSide then
               """"<div class="d"><dt>Each side</dt><dd>Yes</dd></div>"""
           else
               "")
    $"""<article class="item" id="{item.Id}">
<div class="fig">{art}</div>
<div class="ibody">
<p class="chip">{sectionName}</p>
<h3>{esc item.Name}</h3>
<dl class="dose">{dose}</dl>
<dl class="steps">
<div><dt>Start</dt><dd>{esc item.Start}</dd></div>
<div><dt>Move</dt><dd>{esc item.Movement}</dd></div>
<div><dt>Direction</dt><dd>{esc item.Direction}</dd></div>
<div><dt>Return</dt><dd>{esc item.Return}</dd></div>
</dl>
<p class="target"><b>Target:</b> {esc item.Target}</p>
<p class="safety">{esc item.Safety}</p>
</div>
</article>"""

/// Safety gate markup. Every trigger carries its own stop copy as data
/// attributes so the client island (scripts/gate.js) needs no duplicated
/// strings; without JS this renders as an honest static list.
let private gateSection () : string =
    let lis =
        triggers
        |> List.map (fun t ->
            $"""<li data-id="{t.Id}" data-title="{attrEsc t.StopTitle}" data-message="{attrEsc t.StopMessage}">{esc t.Label}</li>""")
        |> String.concat ""
    $"""<section class="gate" data-gate>
<p class="step">Before you start</p>
<h2>Do any of these apply to you right now?</h2>
<ul data-triggers>{lis}</ul>
<div class="stop" data-stop hidden>
<h3 data-stop-title tabindex="-1"></h3>
<p data-stop-message></p>
</div>
<p class="cleared" data-cleared hidden>None of these apply — you can continue to the exercises below.</p>
<p class="gate-note">These need a person, not a web page. If any of them describe you, do not use the exercises today.</p>
</section>"""

/// Page stylesheet. Plain string, deliberately NOT interpolated:
/// CSS braces must stay braces, so this never goes inside $"...".
let private pageCss = """
:root { color-scheme: light; --brand: #1f8ac9; --ink: #1f2937; --ink-2: #475569; --line: #e2e8f0; --bg: #ffffff; --soft: #f8fafc; --warn-bg: #fef3c7; --warn: #92400e; --go-bg: #dcfce7; --go: #15803d; }
body { margin: 0; background: var(--bg); color: var(--ink); font: 17px/1.55 system-ui, sans-serif; }
.wrap { max-width: 760px; margin: 0 auto; padding: 0 20px 64px; }
.banner { background: var(--warn-bg); color: var(--warn); text-align: center; font-size: 14px; padding: 10px 16px; }
header.top { background: var(--brand); color: #fff; padding: 14px 20px; }
header.top b { display: block; font-size: 12px; letter-spacing: .15em; font-weight: 500; opacity: .8; }
header.top span { font-size: 20px; font-weight: 700; }
.demoband { background: #eff6ff; color: #1d4ed8; border-bottom: 1px solid #bfdbfe; padding: 10px 20px; font-size: 14px; }
.back { display: inline-block; margin-top: 18px; color: var(--brand); font-weight: 600; }
h1 { font-size: clamp(30px, 7vw, 40px); letter-spacing: -.02em; margin: 14px 0 8px; }
.lede { color: var(--ink-2); max-width: 56ch; }
.area h2, .acard h3 { letter-spacing: -.015em; }
.area h2 { font-size: 27px; margin: 34px 0 6px; padding-top: 22px; border-top: 1px solid var(--line); }
.count { font-size: 14px; color: var(--ink-2); font-weight: 400; }
.starthere { background: var(--go-bg); color: var(--go); border-radius: 12px; padding: 14px 17px; margin: 20px 0; }
.gate { border: 1px solid var(--line); border-radius: 14px; padding: 18px 20px; margin: 22px 0; }
.step { font-size: 12px; letter-spacing: .14em; text-transform: uppercase; color: var(--ink-2); margin: 0 0 6px; }
.gate h2 { margin: 0 0 10px; font-size: 24px; }
.gate ul { list-style: none; margin: 0 0 4px; padding: 0; display: grid; gap: 7px; }
.gate li { margin: 0; padding: 0; }
.trigger { width: 100%; text-align: left; cursor: pointer; background: #fff; color: var(--ink); border: 1px solid var(--line); border-radius: 11px; padding: 13px 15px; min-height: 48px; font-size: 15.5px; line-height: 1.4; }
.trigger:hover { border-color: var(--warn); color: var(--warn); }
.gate-clear { display: block; width: 100%; cursor: pointer; min-height: 56px; background: var(--brand); color: #fff; border: 1px solid var(--brand); border-radius: 12px; padding: 14px 20px; font-weight: 700; font-size: 17px; margin-top: 12px; }
.stop { background: var(--warn-bg); color: var(--warn); border: 1px solid currentColor; border-radius: 12px; padding: 16px 18px; margin-top: 14px; }
.stop h3 { margin: 0 0 6px; font-size: 20px; }
.stop h3:focus { outline: none; }
.stop p { margin: 0; }
.cleared { background: var(--go-bg); color: var(--go); border-radius: 10px; padding: 11px 13px; margin: 14px 0 0; }
.gate-note { color: var(--ink-2); font-size: 14.5px; }
.acards { display: grid; gap: 12px; margin-top: 18px; }
.acard { display: grid; grid-template-columns: 120px 1fr; gap: 16px; align-items: center; border: 1px solid var(--line); border-radius: 14px; padding: 14px 16px; text-decoration: none; color: inherit; }
.acard:hover { border-color: var(--brand); }
.acard h3 { margin: 0 0 4px; font-size: 20px; }
.acard p { margin: 0 0 6px; color: var(--ink-2); font-size: 15px; }
.acard .n { font-size: 12.5px; color: var(--ink-2); }
.thumb { display: block; background: var(--soft); border: 1px solid var(--line); border-radius: 10px; padding: 6px; }
.thumb svg { width: 100%; height: auto; display: block; }
.item { border: 1px solid var(--line); border-radius: 14px; overflow: hidden; margin: 0 0 18px; }
.fig { background: var(--soft); border-bottom: 1px solid var(--line); padding: 14px; }
.fig svg { width: 100%; max-height: 300px; display: block; }
.ibody { padding: 18px 20px 22px; display: grid; gap: 12px; }
.chip { display: inline-block; justify-self: start; margin: 0; font-size: 11px; font-weight: 600; letter-spacing: .09em; text-transform: uppercase; background: #e0f2fe; color: #0369a1; border-radius: 999px; padding: 3px 10px; }
.item h3 { margin: 0; font-size: 22px; }
.dose { display: flex; flex-wrap: wrap; gap: 1px; background: var(--line); border: 1px solid var(--line); border-radius: 11px; overflow: hidden; margin: 0; }
.dose .d { flex: 1 1 100px; background: var(--soft); padding: 10px 12px; }
.dose dt { font-size: 10px; letter-spacing: .12em; text-transform: uppercase; color: var(--ink-2); }
.dose dd { margin: 0; font-weight: 600; }
.steps { display: grid; gap: 8px; margin: 0; }
.steps div { display: grid; grid-template-columns: 74px 1fr; gap: 10px; }
.steps dt { font-size: 10.5px; letter-spacing: .11em; text-transform: uppercase; color: var(--ink-2); padding-top: 3px; }
.steps dd { margin: 0; }
.target { margin: 0; color: var(--ink-2); border-top: 1px solid var(--line); padding-top: 11px; }
.safety { margin: 0; background: var(--warn-bg); color: var(--warn); border-radius: 10px; padding: 11px 13px; font-size: 15px; }
"""

let private layout (title : string) (scriptPath : string) (body : string) : string =
    $"""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>{title} | Physiotherapy Patient Library (Fable demo)</title>
<style>{pageCss}</style>
</head>
<body>
<div class="banner">NOT MEDICAL ADVICE · STOP IF YOU FEEL SHARP PAIN</div>
<header class="top"><b>PHYSIOTHERAPY</b><span>Patient Library</span></header>
<div class="demoband">Fable demonstration build — content and figures are drafts awaiting clinician review. Not for patient use.</div>
<main class="wrap">
{body}
</main>
<script src="{scriptPath}" defer></script>
</body>
</html>"""

let private areaCard (area : Area) : string =
    let count = itemsForArea area.Id |> List.length
    let itemWord = if count = 1 then "item" else "items"
    $"""<a class="acard" href="{area.Id}/">
{thumbnailFor area.Id}
<div><h3>{esc area.Name}</h3><p>{esc area.Lede}</p><span class="n">{count} {itemWord}</span></div>
</a>"""

/// Home page: banner, gate, area grid.
let renderHome () : string =
    let cards = areas |> List.map areaCard |> String.concat ""
    let body =
        $"""<h1>Exercises and stretches</h1>
<p class="lede">Choose the body area your physiotherapist pointed to. Follow only what they went through with you.</p>
{gateSection ()}
<div class="starthere"><b>Start slowly</b> — perform only the movements your physiotherapist reviewed with you.</div>
<div class="acards">{cards}</div>"""
    layout "Choose a body area" "gate.js" body

/// One area page. Unknown ids fail the build loudly, never a blank page.
let renderArea (areaId : string) : string =
    match areas |> List.tryFind (fun a -> a.Id = areaId) with
    | None -> failwith $"Unknown area \"{areaId}\"."
    | Some area ->
        let cards =
            itemsForArea area.Id |> List.map itemCard |> String.concat ""
        let body =
            $"""<a class="back" href="../">← All areas</a>
<h1>{esc area.Name}</h1>
<p class="lede">{esc area.Lede}</p>
{gateSection ()}
<div class="starthere"><b>Start slowly</b> — perform only the movements your physiotherapist reviewed with you.</div>
{cards}"""
        layout area.Name "../gate.js" body

/// Backwards-compatible single-page render (kept for the demo artifact).
let renderPage () : string = renderHome ()
