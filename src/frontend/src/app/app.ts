import { Component, signal } from '@angular/core';

type Highlight = {
  label: string;
  value: string;
};

type ExperienceItem = {
  title: string;
  period: string;
  description: string;
};

type ProjectItem = {
  name: string;
  stack: string[];
  summary: string;
};

@Component({
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('Abel López');

  protected readonly highlights: Highlight[] = [
    { label: 'Years of experience', value: '4+' },
    { label: 'Projects delivered', value: '12' },
    { label: 'Technologies', value: 'Angular + .NET' },
  ];

  protected readonly focusAreas = [
    'Full-stack product development',
    'Clean architecture',
    'Performance and UX',
    'API integration',
  ];

  protected readonly experience: ExperienceItem[] = [
    {
      title: 'Software Engineer / Full Stack Developer',
      period: '2021 — Present',
      description: 'Building scalable user interfaces, APIs, and internal tools that support business growth and product quality.',
    },
    {
      title: 'Frontend Specialist',
      period: '2019 — 2021',
      description: 'Focused on accessible interfaces, responsive design, and component reusability across business applications.',
    },
  ];

  protected readonly projects: ProjectItem[] = [
    {
      name: 'Cash Harmony',
      stack: ['Angular', '.NET', 'SQL'],
      summary: 'A financial operations platform designed to improve visibility and process management for business workflows.',
    },
    {
      name: 'Personal Portfolio',
      stack: ['Angular', 'ASP.NET Core', 'Telegram'],
      summary: 'A public portfolio and profile site built to showcase experience, projects, and suggestions from visitors.',
    },
    {
      name: 'Operational Dashboard',
      stack: ['Angular', 'REST API'],
      summary: 'A reporting and management dashboard for monitoring business performance and operational KPIs.',
    },
  ];
}
