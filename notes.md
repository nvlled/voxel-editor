Content-Type: text/x-zim-wiki
Wiki-Format: zim 0.6
Creation-Date: 2022-03-29T13:54:57+08:00

====== voxel editor ======
Created Tuesday 29 March 2022

===== What =====
I'm going to create a voxel style world or map editor.
It's like 2D tile editor, only in 3D. Basically,
it will be just a very simple minecraft clone,
but only for map creation purposes.

===== Why =====
First, I don't know how to efficiently use Blender,
and learning it takes time and effort, and
may detract me too much away my current primary goal.
I do plan on learning how to use Blender in the future,
just not now.

===== Why not =====

**3D tile editor**
Of course, there are other existing 3D tile editors,
and suffice to say, they will also take time to
efficiently use and learn. More importantly, some
of them have clunky and unintuitive interface.

**Minecraft level data**
I've considered this as well. There's definitely
a way to parse the minecraft world data, and use
that instead. But it has a lot of cruft and bloat
in it that I don't need. Besides, parsing it
and loading it in my code will probably not be
trivial.

**Voxel editors**
There are a lot of choices for voxel editors,
but a lot don't have linux version, and some
have sketchy NFT links. Goxel seems to be
the most decent one. I would keep looking,
but I'm already getting analysis paralysis
from the choices.

There are two problems with Goxel though.
One is that it only supports colored cubes, not
textured ones. Second is that there's no
way of tagging or adding custom
properties to a group of cubes, for event
triggering and stuffs.

===== That's why =====
So yeah, I have some very specific needs for a voxel
editor that most likely none of the current
voxel editors have. Most importantly, I've always
wanted to create a basic minecraft clone. It should
be a good learning experience, as well as welcome
addition to my would-be portfolio.

===== Specification =====

- simple data representation, ideally just an array of cubes
- cubes can be textured with a plain image atlas
- add cube properties and tags
- create custom cubes
- plain images or textures can be added arbitrarily at any point

====== logs ======
Created Tuesday 29 March 2022

===== Creating a plan =====

Creating a minecraft clone / voxel editor
seems a bit daunting, and there's' a lot
of things to do. Can I do it? Or will I
give up halfway through and seek green
pastures once again?

Well, first things first, basic minecraft clone.

- Toggle grid and show cube coordinates
- Create a function for drawing a textured cube
- Crosshair
- A way to select cube spaces (with or without actual cube)
- A way to insert/remove a cube at select cube space
- Cube collision detection
- Player movement (jumping, flying)

Some general dev tools

- ingame dev console (to avoid need a UI for now)

Then an actual UI if dev console is not enough

- learn and use Raygui

Then for creating custom cubes.

- Open/load image files
- A UI for creating/editing the custom cube

===== Prior Art =====
Tuesday 29/03/2022 18:43

I found two games similar to what I wanted:
Crystal Project and Infinite Mana
I will definitely check these games out.
Well, I'm not really planning on creating
a full RPG, that will take too long. Crystal
Project took 5 years part-time, or 2 years
if worked on full-time. Infinite Mana is not
voxel though, but it still the world definitely
looks nice, nicer even. Actually, HD-2D is
starting to make a trend with games like Triangle
Strategy and those other games that I haven't played.

But, I shouldn't get distracted. I have no hope
of creating visually pleasing games like those.
The reason I chose voxel with 2D sprites is because
it would hopefully make it easier for me to iterate
and prototype games of different variety, not just
topdown RPGs.

===== EODr: End of day reflection =====
Tuesday 29/03/2022 19:31

I didn't really do anything tangible today, just
wrote what's on my mind and what I'm planning
to do. Although it's just an illusion of productivity,
writing things down at least helps me get on the working
mindset. Just to be safe, I should start slow, otherwise
I might relapse back again to sickly state. I will work
if I'm absolutely sure I feel like it. Maybe tomorrow.
What if I'm just procrastinating? No, I can still feel
that I'm not 100% percent recovered yet.

That aside, I just joined another discord server for
gamedev purposes. It was linked from reddit, and slowly
gathered users up to 53. I'm not really expecting anything,
but it might be a good chance to learn to socialize a bit.

===== EODr =====
Thursday 31/03/2022 22:24
[](./Screenshot_2022-03-31_22-21-16.png)
[](./Screenshot_2022-03-31_22-25-04.png)

I actually managed to get something done today. Trivial, but it's still
a step forward. Here's the list of things I did:

- Draw a thick 3D line. At first, I thought it was strange that the raylib DrawLine3D didn't
  have a parameter for line thickness, but I guess it's not as simple as it seems. I looked
  at the source code of DrawLine3D, I was surprised that it draws two triangle to render
  a thicker line. It's probably the fastest way to do that, but how I would do that in 3D?
  The answer? I have no idea. No matter, I want a thicker 3D line for debugging purposes
  only, so any method will do. I just draw several lines together in a tight circle, like
  holding several uncooked pastas together.

  I used the thick 3D line for drawing the origin +x, +y, +z. The +z goes backwards, which
  kind of bothers me. I guess it has something to do with left handed coordinate frame.

  **Edit: **There's actually DrawCylinder, which does what I want.

- Draw 3D text. More surprise, there's no function in the official raylib API to do that,
  but there's an example code that does exactly that. Why was it not included in the API,
  no idea, maybe the implementation is shoddy. I ported the example in C#.
- Finally, the textured cube. First, I rendered a red block, then I iteratively draw
  the sides on the correct position, by trial and error.
  I still haven't done the bottom and top. I'm using
  a large 4MB image atlas for roguelike purposes. I forgot where I got it, but it
  should cover everything I need for prototyping: walls, floors, items, monsters,
  weapons and effects. To my surprise, it actually looks passably alright. Just
  add some lighting and blur filter, and it might actually be pleasant to look at.

All in all, I expect to get a basic minecraft clone working by the day after tomorrow.
I managed to play around for longer hours today, but around the 5 or 6 hour mark,
I started to feel the fatigue. I should cut back to 4 hour for the mean time,
it's more important to have a consistent and predictable work hours, even it
mean less time.

===== EODr: Block insertion =====
Friday 01/04/2022 22:23

[](./Screenshot_2022-04-01_22-22-36.png)

I can now insert blocks (the dark ones in the picture). I thought it was as simple as
using the camera direction to get the block in front of the view, but as it's a lot more
complicated than that. I needed to get the ray from the camera, which is done by more
matrix vodoo. I'll figure how that works when I need to make modifications to that code.

Alas, my problem with the camera code isn't done. The default code for the first-person
view has several issues:

- it has a dumb swinging movement that makes me sick
- it fixes the Y-value at a constant position,
  thus preventing me from making jumping movements
- it even uses a global state, thus makes it
  it difficult to switch to several camera instances
- it has a bug where it changes the camera position when
  I alt-tab, so taking screenshots was frustrating

On the positive side, I get to learn the internals and make
modifications myself. I never thought the day I'd be working
with a C++ codebase would come, but here I am. I guess I
am in between being a game developer and an engine developer,
where I slowly learn both sides of the development. I care
about making games than making engines right now though,
so hopefully I don't get carried away.

===== EODr: Weekend =====
Tuesday 03/04/2022 22:00
I mostly just played Ys VI (again). I've already
played and finished this game at least five times
already, the first playthrough was around 2010,
same with Ys: Oath in Felghana. The only difference
this time is that I actually bought the game and set the
difficulty in hard mode. It's still quite fun to play,
I wish there were more topdown platforming action RPGs.
Ys VIII is fun too, but I still like topdown better for
some reason.

Games aside, I hung out on some discord servers, raylib
and that one gamedev server. One notable conversation
I had was on the raylib channel where someone asked
what was the simplest way to implement 3D physics.
A user named **Jae** responds with "Don't waste your
time blah blah, just use a physics engine".
I thought was a very strong take, so I made contrarian point.
This goes back and forth, for every point he (she?) made,
I made a direct effort to rebut his central point.
Nothing unsual, arguments on the internet happen
all the time at all places. The strange part was
that Jae was quite eager to shrug my points away, by punctuating
his sentences with 🤷, as if to point out that
my arguments were trivial and pointless. It wasn't
offensive, just plain annoying. Was I being judged
on some signal I was unawarely emitting? Was it
obvious that I came from a third world country
and that anything I say was something to be shrugged away?

If she wasn't a girl, he's probably something
that rhymes with his name. The funnier part is that on
the other raylib channel, Jae afterwards posted a link to his
2D physics engine repo. I honestly didn't know what
to make of that. What does it mean? ¯\_(..)\_/¯ Who cares.

===== EODr: Tic Tac Toe =====
Tuesday 04/04/2022 22:00

As I termporary diversion, I tried creating a discord
bot that allows players to play tictactoe. It does this
by sending a different embedded image for each turn.
Players will use a 9x9 buttons to play. I thought that
was a nice hack on the limited discord chat UI, but
after some looking around, I discovered a similar
bot with the very same idea. I dropped the idea for
now, and will come back later if I think of a game
that can be played within the discord UI. I used
discordjs, if it matters. There's a golang library,
but it was undocumented and only had examples.
The examples would have suffice if I already familiar
with the discord bot API.

===== EODr: Replaced camera =====
Tuesday 05/04/2022 22:07

[](./Screenshot_2022-04-05_22-06-48.png)

Nothing significant, I replaced the default raylib camera.
At first, I tried porting the code to C# then modify it.
It didn't work though, I get a blank screen and [[NaN]] coordinates.
I probably made a mistake with the matrix stuffs. I could
redo it again, but carefully this time. Instead, I just
used the code from github.com/raylib-extras/extras-cs.
With some few modifications, and it works.

Also I started the high-level design for the voxel data
representation. I can't really work on the collision
and basic physics without the world representation first.

For now, I played around, inserting cubes everywhere.
It was actually pretty fun for me already, I can't
believe I didn't start a minecraft clone sooner.
I really wanted to make games on minecraft world using
mods, but the API was absolutely atrocious. Well, it
wasn't really that bad, but definitely tedious to learn
and use. If I had to put that much effort, why
not expend that energy in learning a proper game engine
or framework.

I also stumbled upon some youtube videos where gamedevs show-off
their awesome voxel world implementations. I wish I could
learn from those videos, but at most it only serves to show
how amazing those gamedevs were. Well, I did learn
about marching cubes, I definitely could use it later on.
Truth be told, I felt a bit demotivated when I compare
the shit I'm working on. But no, I won't get it to my head.
I have very specific goals in mind, and it doesn't
involve impressing people.

===== EODr: Atlas viewer =====
Wednesday 06/04/2022 22:14

[](./Screenshot_2022-04-06_22-13-40.png)

Since the atlas image is too big for me to manually count the row and column number
of a tile, I wrote a tool to easily do that for me. Pretty sure there's an existing
tool for that, luckily it only took me an hour or three to finish it. I tried creating
a similar tool in lua/love before, but that took way too long and I didn't even bother
finishing it. It was partly due to my ill state, and partly because lua has a very
limited tooling and libraries, so I ended up rewriting a lot of things from scratch.
The part that was tedious when it comes to lua was write access to file system,
and serializing to and from JSON.

In addition to raylib, .NET has a good standard library so a lot of things are done for me.
I also took the chance to try out ImGUI, and to my surprise, and I didn't have to bend
backwards to get it working. Hooray for libraries with good API, definitely makes my
life easier. I think I just found my ideal game/tool development platform: C# + raylib + .NET + ImGUI

Of course, I don't think I can use ImGUI for actual game interface, but it's a quick way
of adding developer interface and tools.

===== EODr: Something =====
Thursday 07/04/2022 22:32

No visual changes, I just kept working on the world representation part,
and the atlas manager. I need to rework some parts, I realize, otherwise
some cubes will be randomly changing between a set of textures per frame.
That will likely look disturbing. I wrote an atlas manager, which given
a tileName, will give me a texture and a region (x,y,width,height) for
drawing the tile. Since there's a good chance I will be using multiple
atlas image, it's a good abstraction to prepare in advance. I would
have a function like InsertCube("grass", pos). For the world data, I
will be using a cube of array, as I planned before. I could use a sparse
array if ever it consumes too much memory.

To convert a 3d vector to an index for the cube array, I did something like
**x\***(ySize\*zSize) + **y\***zSize + **z**

Since index is an int, I could have a world size of something like 1000x1000x1000,
which is more than enough for level editor.
I won't be making an infinite voxel world. It will be split into levels, like
regular older games. Of course, I wanted to have negative values for the coordinates too,
so I some more tweaking to the expression above.

As I side note, I found a nice japanese learning site https//tatsumoto.neocities.org
The nice part is that it's aimed at tech people (particularly at linux dorks like me),
and the text is sufficiently technical that it's straightforward to extract needed
information and follow the instructed steps. The author seemed to have done his
research well, and I will just have faith in the authencity and effectiveness of
the content. It's based around on spaced repetition and comprehensible input
anyway, a topic that I've done my own reading as well.

It was tedious to setup fcitx, but now I can write japanese
on a en-US keyboard ケケケ.
Well, write japanese scripts anyway, not actual japanese.

===== End of yesterday reflection =====
Saturday 09/04/2022 11:55

It's weekend so I will avoid doing any programming,
or any activity that disturbs my unwaking hours.
I finally got a roughly working implementation for the
voxel world. There were a few bugs such as the
cube texture doesn't show up, but those are
trivial to fix. The problem? The naive implementation,
where I iterate all the cubes in the world in each frame
for rendering, is slow. I expected it to be slow, but it was basically
crawling. Even with a small 100x100x50 world, it would
have to iterate half a million cubes for each loop.

Good thing I'm using a old laptop, otherwise I wouldn't have
noticed and settled for such abomination.

The solution is simple: just render the cubes where the camera is facing.
I tried the first thing I had in my mind. Imagine a cone that extends
out where the camera is facing. For simplicity, I just went with a
squarish cone, so a pyramid or frustrum. For each step in the camera ray,
I render a wall of cube, with a larger wall for each step.

There are two problems with this though. First is that it's only
relatively fast up to a certain render distance, around 20. Second,
Some cubes won't render at certain camera angles. It has no problem
rendering all the cubes provided that the camera is aligned with the
xy-, xz- or yz plane. Random cubes disappearing is annoying and
unnacceptable. Basically, broken.

I tried adding a fix, but it either made it too slow, or only made
the bug less likely to happen, but it's still there. Also, a render
distance of 20 is too small. Even a small house wouldn't render properly.
In other words, this implementation will not work.

The night before, my brain tried to come up with a solution against
my bidding as I try to sleep. I went over the alternative solutions,
and was convinced that they won't work. Finally, to put myself at
peace, I convinced myself that raycasting would be the acceptable
solution. I will do raycasting, but with cubes instead of pixels.

Of course, a saner practical person will just read up on resources
on how voxel worlds are typically implemented. But that's not very
fun. I'd like to at least think of my own shitty solutions first.
On the plus side, I get to appreciate more the tools and math more.
Until now, I never have a reified understanding of cross product
and dot product. I read several linear algebra books and I still
didn't see the point of it. Even visual and detailed explanations
of 3D math fell short until I tried implementing them.

In short, I'm learning more with practical projects. Well, not
quite. It's a huge disservice not to give credit to the pirated
books that I read. Reading and applying is both important.

I have to remind myself again though. Learning is good and all,
but I need to have an actual output for this project. A level
editor that fits my use case, and more importantly, actual
games.

===== EODr: Looking at cubes =====
Tuesday 12/04/2022 22:40

I've made a lot of changes, but most of it is related
to rendering the cubes. To recap, I've gone through
several phases of rendering the cubes:

1. render all cubes in the world for each frame
   This really doesn't work except for really, really small worlds

2. render all the cubes inside the camera frustrum
   Basically, a pyramid, with the tip or apex near the camera,
   and the pyramid base at the far end. It also doesn't work, it misses
   a lot cubes at angles not parallel to standard xy, xz, yz planes.

3. render all the cubes that are raycasted
   Similar to ②, but different iteration order. Like I said,
   raycasting, but with cubes instead of pixels. Tricky to get
   right, and it seemed to work fine, until I randomly added
   more cubes. The cubes near the camera ray are rendered correctly,
   but rest are unpredictably rendered. The result is annoying
   flickering cubes for every little camera movement.
   This is an example that seems good in theory, but actually
   terrible in practice. I could do some more tweaking, but
   again, it'll come at a cost of redundantly looping over
   more cubes.

4. render all raycasted cube chunks (implementation not yet started)
   Similar to (3), but with cube chunks (e.g. 16x16x16) instead
   of individual cubes. This should solve the problem of flickering cubes,
   as well as cut the number of iterated cubes per frame. In hindsight,
   I really didn't need to raycast individual cubes since I don't need
   the pixel-like accuracy, I just need the blocks rendered in the general
   direction of the camera.

I could have probably saved some time if I just skipped right to (4),
I guess I underestimated the nuances and complexity of rendering cubes,
thinking that a naive implementation would be enough. How wrong I was.

Aside from choosing which cube to render,
there was also other problems with the rendering the textures itself.

First, rendering the cube more than a thousand times in a frame, results
in a significant frame drop, even it's the same cube rendered over and
over. Finding the source of the frame drop wasn't straightforward.
I initially thought it was because of too much allocations, or the
value copy semantics of C# that I didn't get right. But no, it was
because there was too many calls to drawing.
This is surprising, because in other graphic libraries I've used,
texture rendering is automatically batched, so rendering them once
or a thousand times wouldn't make a difference. The fix is to make
sure a cube is only rendered once per frame.

Second problem is that when too many cubes rendered, say around half a thousand,
breaks raylib. This is again surprising, even an old igpu could handle
half a hundred thousand texture drawing with ease. The problem is of course,
the rlgl is a low-level library, meaning I have to be careful and precise
on how it's used. Leaky abstractions and all. Maybe I got my expectations
wrong, but graphics libraries are supposed to do these for me, so that
I don't have to think about them and focus on my high(er)-level code. I
might as well use opengl directly. But I still have not yet learned
opengl, so there's that.

Fortunately, looking at the raylib source, I was able to find a work-around
and fix the problem. Hopefully. I have already invested effort learning
and using in raylib, so it's a bit demotivating to move another game library
when I haven't finished a single game.

===== EODr: Visual tests =====
Thursday 14/04/2022 22:24

So I started making small changes for the chunky implementation.
Then lots of things broke. It's not much a problem of modularization
and encapsulation, but little bugs just accumulated, which is to
say, I haven't really tested some of the subfunctions or helper functions
if they work. In particular, if the rays are pointing at the right direction
and have the proper length.

This calls for automated testing? Nope, I wouldn't know how do right proper tests
for that. Instead, I wrote visualization functions to see if the rays are
indeed correct. As it turns out, it's broke and it was a bit tricky to fix.
The problem: given a renderDistance (say 50), get the size of the rectangle
that is 50 units away from the where camera is standing and pointing, such
that the rectangle fits roughly the camera view size.

My initial implementation was based on intuition. I have the fov (camera view angle)
and the renderDistance. I get a vector v = cameraDirection \* renderDistance.
I get another vector w = (rotate v by fov 2pi). Then I could get the rectangle width
or height by length(w - v). Of course, I need to rotate v by the side axis
and the front axis, which means more cross product vodoos. I also got the chance
to use quaternions for the first time. Contrary to what I expected, I actually
didn't need to consult a math book to be able to use a quaternion. I mean, I tried,
but it all went over my head. Luckily, it's already implemented in raylib,
and I didn't to concern myself with the underlying details (unlike the rlgl).

Did it work? Well yes, but actually no. In some cases of renderDistance and fov,
the visual lines snuggly fits in the camera, but in other cases, it's either
too small or too big. Too small means a lot of cubes won't be seen, too big
means wasted cycles rendering cubes that I can't see anyway.

So I did some rethinking. Then I realized I'm essentially computing a side
of a right triangle. Sohcahtofu or something. I recall reading from a book
that I can't particularly remember, that a lot of 3D problems can be solved
in 2D. Thanks, 3D math book. I used the following formula:

renderDistance/tan(fov)

Did it work? Yup, but actually nope. It only worked in somes cases.
What am I doing wrong? I have no idea. I skimmed the stash of pirated
math books and 3d engine books for clues. I couldn't find a relevant formula.
Then I searched for formulas related to frustum and cones. Most of them
includes the computing the volume or area, which I can't because I need the radius
(or in my case the width or height) to get the volume or area.

After some stressful searching, I found a wolfram page about cones:
{{./NumberedEquation1.svg}}

I have the h (renderDistance) and the θ (the fov), so that means
I could get the r (the width or height). With my shitty algebra-fu, I get
the following:

tan(fov/2) \* renderDistance

It looks somewhat similar to the triangle equation.
Did it work? To my surprise, yes! All this time
I felt like I have no idea what I'm doing, but
I found a proper solution anyway. Pretty cool
and satisfying. All the rays are pointing
in the right direction too, the way god
intended them to be, like rays shining from the heavens above.

Wait, instead of game development,
I feel like I'm doing some math homework found
at the last few chapters of an high school textbook.

===== EODyr: It's still broke =====
Saturday 16/04/2022 21:56

I've been programming for more than 10 years now (well,
most of it are personal or hobby), but I still get bitten
by really obvious bugs that I could have prevented if
I took a minute or two reviewing the code I just written,
before doing a test run.

Yesterday, I spent a good portion of my time scratching my
head. There's nothing more frustrating than having a good mental
of my code, and it does nothing like what I had in mind.
Did I make an assumption that turned out to be false?
Is there an edge case that I missed?
Was I too tired to see the obvious errors in front of me?

The bug: more blinky cubes. The slightest camera movement
would make a whole chunk of cubes appear and disappear.
But it shouldn't happen anymore. I should know, there are like
10 rays passing through a chunk, and it still misses it.
I tweaked the parameters, casting redundant rays and making
the ray move at smaller steps. Nothing's fixing it.

The whole codebase is still a mess, I move fast and loose,
adding and removing code regularly. There's lot of commented
out code. Where-ever the bug was, finding it by going through
the codebase wasn't going to be easy. Instead, I started
at small steps. Render only several cubes near the origin.
It looks fine, but wait, some cubes are already missing,
and it won't appear no matter what the camera angle is.
Particularly, a cube at (-2, 0, 0) wont render, but
a cube at (-1, 0, 0) shows up fine. The heck?

After some more shitty detective work, I narrowed down
where the problem is. I briefly looked at the code,
wondering why it's not working, then it hit me like
a weeb getting hit by an isekai truck.

It's the code that returns a list of cube positions
given a chunk position. Most of it are fine, but
I forgot to multiply the chunk position by the chunk size.
So, if the chunk size was 10*10*10, then the returned
cube position would be off at least by 10, at most by 10*10*10,
which is huge. It explains why a chunk won't render,
because the cubes are rendered in the wrong place.

The moral of the story: keep the codebase relatively clean,
add a lot of asserts, and allocate at least 5 minutes reviewing
the added or modified
code. What about automated testing? Dunno, writing tests
isn't fun and would just ruin my momentum. Plus, I frequently
change a lot of code, it's too early for tests. I believe
asserts should suffice in place of tests.

===== EODr: It's still somewhat slow =====
Monday 18/04/2022 22:20

Rendering several cubeful of chunks significantly
drops the framerate. On 20*20*20 chunk size, the framerate
drops to around 10 FPS. I have two optimizations that should
solve this problem, but first I tried rendering the cube
with meshes and see if it improves.

It didn't. I thought there's an built-in functionality in
raylib or opengl that would automatically do render culling,
or something like that. It doesn't come for free, it seems,
and I have to manually detect which cubes and which sides
of the cubes should be drawn. I mean, there's probably a
more efficient approach, but it would mean that I have to
learn opengl for real. That, or I really just need to
get decent GPU. Or not, I can play minecraft just fine
on this old laptop. If I can get a stable 50-60 FPS with
a thousands of cubes in sight, then I'd be more than happy
to move on to the physics and collision detection part.
I'd likely cap the framerate to 30 FPS in the later part
of the development when I start adding lighting or particles.

To avoid drawing the sides of the cube that are not visible,
I would use surface normals. Normal vector is another linear
algebra concept that I trouble seeing the purpose or why
it's used that way, until I encounted a probem where they
are applicable. I didn't even need to look anything up, it just
occured to me, hey, I should use normals here. Or maybe
I've read somewhere that visibility is normally solved
with normal vectors. Maybe, I really can't recall. I suppose
I should give credit to my ability to absorb information,
even at most times, I feel like it went way over my head.
I looking forward to a time when matrices would feel
natural to use as well.

As for avoiding the inner cubes of the chunk that aren't
visible because they are covered by the outer cubes, I
would just once again use ray casting.

===== EODr: Drawing visible sides and in-game console =====
Wednesday 20/04/2022 22:04

Yesterday, I made changes so that only the visible sides of a cube
are rendered. Just from this change alone signficantly improved
the framerate. I can now render a 20x20x20 cubeful of chunk at
a stable 60 FPS. Alas, this is not enough. I played around a bit,
and added a lot of cubes and it drops around 45 FPS. Which is expected,
since the change I've made only halves the draw rate. If there's too
many cubes in the camera direction, then it still draws a lot.

I could go on trying to make it a bit more faster, but no. I'll never
get anything done that way. I did fix some more bugs yesterday,
but that's it. No more optimizations, no more minor bug fixes.
Today, I planned out what to do next. I started making the in-game
console. I tried creating the view using only raylib API, but
it's too tedious.
I'm going to use [[ImGUI]] instead.

===== End of Past Week reflection: =====
Monday 02/05/2022 12:10

==== Malformed triangles are the bane of suffering ====

Right triangle, or wrong triangle?

Last week, the final week of the month, wasn't very productive.
The week started with me feeling sick or tired. I wasn't sure
what was the cause, but one thing was certain: I started
to eat more carbs, specifically bread-like foods like sandwhiches
pancakes, and crackers. Exerting my mind and body more gave
a craving to eat more, it seems. And perhaps stress. I did
recall saying to myself, oh no, it's the end of the month,
I should wrap it up and move on to the next stages.

Could it be that I have intolerance to breadlike foods?
I have my suspicions for a while, but I'm not really sure.
After days of avoiding such foods, I did feel way better.
Intolerance or not, I should avoid excessive eating
of any kind of food anyway. It's a shame though,
coffee and sandwhich are one of the few joys of my life.
How do I spell sandwhich again? Sandwhich, sandwich, sandwitch?
Looking it up, it's sandwich. The other two are common mispellings.

And as if my executive areas of my brain were malfunctioning,
I started to work on an optimization instead of continuing
work on the console and world persistence. In my mind,
this should be easy, it's just a little change, it
wouldn't take too long. In just a few hours, I should
be done.

It took me a week. It's at most twenty lines of code,
but it took a week to implement, and hundreds of lines
of shitty trial-and-error code. Reflecting on it,
I definitely wasn't in the working condition. I made
the most basic programming errors which compounded
the underestimated difficulty of the problem.

The optimization: instead of stepping over the ray
at discrete steps (say 1 unit per step), it would
iterate the ray at variable step, from one chunk
and directly towards the immediate next chunk.
If the next chunk is only 0.5 unit away, it would
take 0.5 unit. If it was 10 units away, it would take
10 unit steps. This would significantly reduce
the number of loops. Before, I set it to 1 unit
per step since anything larger than that and
it misses a chunk sometimes.

The problem: it's easy to describe the problem
in paper. Since it's hard to sketch 3D cubes
in paper, I used 2D quads instead, but
same principles apply.

The solution: at a very-high level plain
english description, it's also easy to describe the solution.
Except when it comes to expressing the steps
precise and programmatically, the solution was not
so obvious. In the end, if there are angles
and lengths, it all comes down to finding right
triangles to solve. I wish I have more sophisticated
math tricks to apply, but I'm just a primitive caveman
with right triangle tools to solve most problems.

The bigger problem: I wasn't feeling very well,
my working memory was compromised to fully visualize
a working solution. And it shows. At times, I'd
say, what's this variable for again? What was I
doing again? The worst part is I kept on trying
to go with the more complicated solution,
instead of going with the simpler straightforward one.
The got where I got stuck the most was where
I used a wrong vector as a side of the triangle.
In my mind, it was a right triangle, but it was
actually anything but.

Around the later parts of the week, I started
to feel better, and I was able to rework
a simpler solution: given a point inside the quad
and a vector pointing to the next quad,
get the nearest side (or plane in 3D)
and create a triangle from that. The actual
algorithm is a little convoluted to describe
in plain words, maybe I'll edit this later.
One minor gripe I had is that raylib's GetRayCollisionQuad()
doesn't seem to be always working. Looking at the
code comments, it says something about
the points in counter-clockwise direction,
but I was puzzled which direction was it referring to.
Counter-clockwise is clockwise if viewed from the
opposite direction. Forget about GetRayCollisionQuad,
I manually raycasted using loop and not math equations.

The result: it works, but at what cost?
Well, a week of frustration. But on the other hand,
stable 60 FPS. Hurray!? Even better, I discovered
that running the program using the release
binaries (not the debug one) runs even faster.
There's one more final (I mean really final) optimization
I have in mind, and it should solve all the frame drops,
and I'm actually itching to implement it. I swear
to my future self and to my hypothetical decendants,
that this is easier and won't take long.

But wait, I have something else [[../../game-anki.txt|to do.]]

===== End of month reflection =====

...
