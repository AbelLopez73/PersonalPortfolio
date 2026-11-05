import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { App } from './app';
import { AdminPresenceService } from './admin-presence.service';
import { OllamaAssistantService } from './ollama-assistant.service';

describe('App', () => {
  const adminPresence = {
    state: signal<'checking' | 'connected' | 'unavailable'>('unavailable'),
    isAdmin: signal(false),
    loginEnabled: signal(false),
    loginUrl: 'http://localhost:5016/api/admin/login',
    initialize: async () => undefined,
    logout: async () => undefined,
  };
  const assistant = {
    chat: () => of({ reply: 'Hello from Ollama.' }),
  };

  beforeEach(async () => {
    adminPresence.state.set('unavailable');
    adminPresence.isAdmin.set(false);
    adminPresence.loginEnabled.set(false);
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        { provide: AdminPresenceService, useValue: adminPresence },
        { provide: OllamaAssistantService, useValue: assistant },
      ],
    })
      .compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render title', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Abel López');
  });

  it('should show Telegram sign-in only when the server enables it', async () => {
    adminPresence.loginEnabled.set(true);
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    fixture.detectChanges();

    const loginLink = fixture.nativeElement.querySelector('.admin-action') as HTMLAnchorElement;
    expect(loginLink.textContent).toContain('Sign in with Telegram');
    expect(loginLink.href).toBe(adminPresence.loginUrl);
  });

  it('should show admin session status and sign-out when authenticated', async () => {
    adminPresence.isAdmin.set(true);
    adminPresence.state.set('connected');
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('.admin-status')?.textContent).toContain('Admin session online');
    expect(compiled.querySelector('.admin-action')?.textContent).toContain('Sign out');
  });
});
