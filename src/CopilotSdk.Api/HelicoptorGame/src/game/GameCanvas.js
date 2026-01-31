import React, { useRef, useEffect } from 'react';

const WIDTH = 800;
const HEIGHT = 480;

export default function GameCanvas({ onScore }) {
  const canvasRef = useRef(null);
  // Game state
  const state = useRef({
    helicopter: {
      x: 100,
      y: HEIGHT - 80,
      vy: 0,
      vx: 0,
      gunAngle: 0,
      onPad: true,
    },
    bullets: [],
    targets: [],
    score: 0,
    keys: {},
    frame: 0,
    landscapeOffset: 0,
  });

  // Keyboard controls
  useEffect(() => {
    const handleDown = (e) => {
      state.current.keys[e.code] = true;
      if (e.code === 'Space') e.preventDefault();
    };
    const handleUp = (e) => {
      state.current.keys[e.code] = false;
    };
    window.addEventListener('keydown', handleDown);
    window.addEventListener('keyup', handleUp);
    return () => {
      window.removeEventListener('keydown', handleDown);
      window.removeEventListener('keyup', handleUp);
    };
  }, []);

  // Main game loop
  useEffect(() => {
    let running = true;
    const ctx = canvasRef.current.getContext('2d');
    function loop() {
      update();
      render(ctx);
      if (running) requestAnimationFrame(loop);
    }
    loop();
    return () => { running = false; };
  }, []);

  // Game update logic
  function update() {
    const s = state.current;
    // Helicopter controls
    if (s.keys['ArrowUp']) {
      if (s.helicopter.onPad) s.helicopter.onPad = false;
      s.helicopter.vy -= 0.3;
    }
    if (s.keys['ArrowDown']) s.helicopter.vy += 0.3;
    if (s.keys['ArrowRight']) s.helicopter.vx += 0.15;
    if (s.keys['ArrowLeft']) s.helicopter.vx -= 0.15;
    // Gun aiming
    if (s.keys['ShiftLeft'] && s.keys['ArrowUp']) s.helicopter.gunAngle = Math.max(s.helicopter.gunAngle - 0.03, -0.7);
    if (s.keys['ShiftLeft'] && s.keys['ArrowDown']) s.helicopter.gunAngle = Math.min(s.helicopter.gunAngle + 0.03, 0.7);
    // Physics
    s.helicopter.vy += 0.15; // gravity
    s.helicopter.vx *= 0.99; // drag
    s.helicopter.vy *= 0.98;
    s.helicopter.x += s.helicopter.vx;
    s.helicopter.y += s.helicopter.vy;
    // Clamp to screen
    if (s.helicopter.y > HEIGHT - 60) {
      s.helicopter.y = HEIGHT - 60;
      s.helicopter.vy = 0;
      s.helicopter.onPad = true;
    }
    if (s.helicopter.y < 40) {
      s.helicopter.y = 40;
      s.helicopter.vy = 0;
    }
    // Fire bullets
    if (s.keys['Space'] && !s.keys._fired) {
      s.bullets.push({
        x: s.helicopter.x + 60 * Math.cos(s.helicopter.gunAngle),
        y: s.helicopter.y + 10 + 60 * Math.sin(s.helicopter.gunAngle),
        vx: 8 * Math.cos(s.helicopter.gunAngle) + s.helicopter.vx,
        vy: 8 * Math.sin(s.helicopter.gunAngle) + s.helicopter.vy,
      });
      s.keys._fired = true;
    }
    if (!s.keys['Space']) s.keys._fired = false;
    // Move bullets
    s.bullets.forEach(b => {
      b.x += b.vx;
      b.y += b.vy;
    });
    s.bullets = s.bullets.filter(b => b.x < s.helicopter.x + WIDTH && b.x > s.helicopter.x - 100 && b.y > 0 && b.y < HEIGHT);
    // Scroll landscape
    s.landscapeOffset += s.helicopter.vx;
    // Spawn targets
    if (s.frame % 80 === 0) {
      const tx = s.helicopter.x + WIDTH + Math.random() * 200;
      const ty = 80 + Math.random() * (HEIGHT - 160);
      s.targets.push({ x: tx, y: ty, hit: false });
    }
    // Move targets
    s.targets = s.targets.filter(t => t.x > s.helicopter.x - 100);
    // Collision detection
    s.bullets.forEach(b => {
      s.targets.forEach(t => {
        if (!t.hit && Math.abs(b.x - t.x) < 18 && Math.abs(b.y - t.y) < 18) {
          t.hit = true;
          s.score += 100;
          if (onScore) onScore(s.score);
        }
      });
    });
    s.targets = s.targets.filter(t => !t.hit);
    s.frame++;
  }

  // Game rendering
  function render(ctx) {
    const s = state.current;
    ctx.clearRect(0, 0, WIDTH, HEIGHT);
    // Draw sky
    ctx.fillStyle = '#87ceeb';
    ctx.fillRect(0, 0, WIDTH, HEIGHT);
    // Draw hills
    ctx.fillStyle = '#228B22';
    ctx.beginPath();
    ctx.moveTo(0, HEIGHT);
    for (let x = 0; x <= WIDTH; x += 8) {
      const wx = s.helicopter.x - 100 + x + s.landscapeOffset * 0.2;
      const y = HEIGHT - 40 - 30 * Math.sin(wx * 0.008) - 18 * Math.cos(wx * 0.021);
      ctx.lineTo(x, y);
    }
    ctx.lineTo(WIDTH, HEIGHT);
    ctx.closePath();
    ctx.fill();
    // Draw helipad
    ctx.fillStyle = '#888';
    ctx.fillRect(80 - s.helicopter.x + 100, HEIGHT - 50, 80, 10);
    // Draw helicopter
    drawHelicopter(ctx, s.helicopter, s.helicopter.x - s.helicopter.x + 100, s.helicopter.y);
    // Draw bullets
    ctx.fillStyle = '#222';
    s.bullets.forEach(b => {
      ctx.beginPath();
      ctx.arc(b.x - s.helicopter.x + 100, b.y, 4, 0, 2 * Math.PI);
      ctx.fill();
    });
    // Draw targets
    s.targets.forEach(t => {
      ctx.fillStyle = t.hit ? '#aaa' : '#f00';
      ctx.beginPath();
      ctx.arc(t.x - s.helicopter.x + 100, t.y, 18, 0, 2 * Math.PI);
      ctx.fill();
      ctx.strokeStyle = '#fff';
      ctx.stroke();
    });
    // Draw score
    ctx.fillStyle = '#fff';
    ctx.font = '20px monospace';
    ctx.fillText('Score: ' + s.score, 20, 30);
  }

  function drawHelicopter(ctx, heli, x, y) {
    // Body
    ctx.save();
    ctx.translate(x, y);
    ctx.fillStyle = '#333';
    ctx.fillRect(-30, -10, 60, 20);
    // Rotor
    ctx.fillStyle = '#666';
    ctx.fillRect(-35, -18, 70, 6);
    // Skids
    ctx.strokeStyle = '#222';
    ctx.beginPath();
    ctx.moveTo(-20, 14); ctx.lineTo(20, 14);
    ctx.moveTo(-18, 18); ctx.lineTo(18, 18);
    ctx.stroke();
    // Gun
    ctx.save();
    ctx.translate(30, 0);
    ctx.rotate(heli.gunAngle);
    ctx.fillStyle = '#555';
    ctx.fillRect(0, -3, 30, 6);
    ctx.restore();
    ctx.restore();
  }

  return <canvas ref={canvasRef} width={WIDTH} height={HEIGHT} tabIndex={0} style={{border:'2px solid #333', background:'#222', display:'block', margin:'32px auto'}} />;
}
