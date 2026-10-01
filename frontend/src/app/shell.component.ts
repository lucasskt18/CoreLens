import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="app">
      <header class="topbar">
        <a routerLink="/" class="brand">
          <span class="mark" aria-hidden="true"></span>
          <span class="name">CoreLens</span>
        </a>
        <nav>
          <a routerLink="/" routerLinkActive="on" [routerLinkActiveOptions]="{ exact: true }">Ao vivo</a>
          <a routerLink="/history" routerLinkActive="on">Histórico</a>
        </nav>
      </header>
      <main class="content">
        <router-outlet />
      </main>
    </div>
  `,
  styles: [`
    .app {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }
    .topbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      padding: 14px 28px;
      border-bottom: 1px solid var(--line);
      background: color-mix(in srgb, var(--bg) 88%, var(--card));
      position: sticky;
      top: 0;
      z-index: 10;
    }
    .brand {
      display: flex;
      align-items: center;
      gap: 10px;
      min-width: 0;
    }
    .mark {
      position: relative;
      width: 28px;
      height: 28px;
      border-radius: 8px;
      border: 1px solid var(--line-strong);
      background: var(--card-elevated);
      flex: none;
    }
    .mark::before {
      content: "";
      position: absolute;
      inset: 7px;
      border-radius: 50%;
      border: 1.5px solid var(--accent);
    }
    .mark::after {
      content: "";
      position: absolute;
      inset: 11px;
      border-radius: 50%;
      background: var(--accent);
      opacity: 0.55;
    }
    .name {
      font-size: 14px;
      font-weight: 600;
      letter-spacing: -0.02em;
    }
    nav {
      display: flex;
      gap: 6px;
    }
    nav a {
      padding: 6px 12px;
      border-radius: 999px;
      font-size: 13px;
      color: var(--muted);
      border: 1px solid transparent;
    }
    nav a:hover {
      color: var(--text);
      background: var(--card);
    }
    nav a.on {
      color: var(--text);
      background: var(--card);
      border-color: var(--line);
    }
    .content {
      flex: 1;
    }
    @media (max-width: 720px) {
      .topbar { padding: 12px 20px; }
    }
  `]
})
export class ShellComponent {}
