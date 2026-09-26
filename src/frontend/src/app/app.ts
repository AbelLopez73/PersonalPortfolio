import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { AdminPresenceService } from './admin-presence.service';

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

type ProfileVariant = {
  id: string;
  label: string;
  title: string;
  summary: string;
  focusAreas: string[];
  highlights: Highlight[];
  experience: ExperienceItem[];
  projects: ProjectItem[];
  resumeLabel: string;
  resumeUrl: string;
};

@Component({
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App implements OnInit {
  protected readonly title = signal('Abel López');
  protected readonly adminPresence = inject(AdminPresenceService);

  protected readonly variants: ProfileVariant[] = [
    {
      id: 'software-engineer',
      label: 'Software Engineer',
      title: 'Software Engineer',
      summary:
        'I design and build resilient digital products that combine clean user experiences with strong backend foundations.',
      focusAreas: [
        'C# API with Angular',
        'VB.NET with APIs',
        'Python automation and services',
        'IBM i / RPG / ILE',
        'SQL Server and database design',
        'Other stack: PHP, ASP.NET, JavaScript',
      ],
      highlights: [
        { label: 'Years of experience', value: '4+' },
        { label: 'Core stack', value: 'C# + Angular' },
        { label: 'Specialties', value: 'Legacy + modern' },
      ],
      experience: [
        {
          title: 'Software Engineer / Full Stack Developer',
          period: '2021 — Present',
          description: 'Building scalable interfaces, APIs, and internal tools with modern web technologies and backend services.',
        },
        {
          title: 'Legacy systems and business apps',
          period: '2019 — 2021',
          description: 'Supporting business workflows through integration, maintenance, reporting, and process automation across enterprise platforms.',
        },
      ],
      projects: [
        {
          name: 'Cash Harmony',
          stack: ['Angular', '.NET', 'SQL'],
          summary: 'Operational platform focused on business clarity, workflow visibility and process improvement.',
        },
        {
          name: 'Portfolio + contact hub',
          stack: ['Angular', 'ASP.NET Core', 'Telegram'],
          summary: 'Public profile, project showcase and suggestion workflow with external communication.',
        },
        {
          name: 'Enterprise integrations',
          stack: ['C#', 'VB.NET', 'SQL Server'],
          summary: 'Service orchestration and integration layers connecting business systems and reporting flows.',
        },
      ],
      resumeLabel: 'Software Engineer Resume',
      resumeUrl: '#',
    },
    {
      id: 'warehouse-associate',
      label: 'Warehouse Associate',
      title: 'Warehouse Associate',
      summary:
        'I contribute to efficient warehouse operations through organization, process discipline, inventory control and reliable task execution.',
      focusAreas: [
        'Inventory control',
        'Logistics support',
        'Shipping and receiving',
        'Safety and organization',
        'Operational reliability',
      ],
      highlights: [
        { label: 'Operations', value: 'Inventory' },
        { label: 'Mindset', value: 'Efficiency' },
        { label: 'Focus', value: 'Safety + accuracy' },
      ],
      experience: [
        {
          title: 'Warehouse support and inventory processes',
          period: 'Operations experience',
          description: 'Supporting stock organization, product movement, order flow, and continuous process improvement in fast-paced environments.',
        },
      ],
      projects: [
        {
          name: 'Material flow optimization',
          stack: ['Inventory', 'Operations', 'Safety'],
          summary: 'Improving consistency and accuracy in warehouse movement, support and stock organization.',
        },
      ],
      resumeLabel: 'Warehouse Resume',
      resumeUrl: '#',
    },
    {
      id: 'professional-driver',
      label: 'Professional Driver',
      title: 'Professional Driver',
      summary:
        'I provide dependable driving services with attention to safety, punctuality, logistics discipline and customer care.',
      focusAreas: [
        'Route reliability',
        'Safety compliance',
        'Time management',
        'Customer communication',
        'Logistics operations',
      ],
      highlights: [
        { label: 'Priority', value: 'Safety' },
        { label: 'Strength', value: 'Punctuality' },
        { label: 'Care', value: 'Customer service' },
      ],
      experience: [
        {
          title: 'Professional driving and route support',
          period: 'Logistics experience',
          description: 'Delivering reliable transport services with strong discipline in route execution, communication and safe operation.',
        },
      ],
      projects: [
        {
          name: 'Transport and delivery support',
          stack: ['Logistics', 'Safety', 'Timing'],
          summary: 'Reliable support for timely delivery and safe, customer-focused service execution.',
        },
      ],
      resumeLabel: 'Driver Resume',
      resumeUrl: '#',
    },
  ];

  protected readonly selectedVariantId = signal('software-engineer');

  protected readonly currentVariant = signal(this.variants[0]);

  protected readonly focusAreas = computed(() => this.currentVariant().focusAreas);
  protected readonly highlights = computed(() => this.currentVariant().highlights);
  protected readonly experience = computed(() => this.currentVariant().experience);
  protected readonly projects = computed(() => this.currentVariant().projects);
  protected readonly variantTitle = computed(() => this.currentVariant().title);
  protected readonly variantSummary = computed(() => this.currentVariant().summary);
  protected readonly resumeLabel = computed(() => this.currentVariant().resumeLabel);
  protected readonly resumeUrl = computed(() => this.currentVariant().resumeUrl);

  ngOnInit(): void {
    void this.adminPresence.initialize();
  }

  protected selectVariant(variantId: string): void {
    const variant = this.variants.find(item => item.id === variantId) ?? this.variants[0];
    this.selectedVariantId.set(variant.id);
    this.currentVariant.set(variant);
  }

  protected logoutAdmin(): void {
    void this.adminPresence.logout();
  }
}
