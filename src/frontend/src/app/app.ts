import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { AdminPresenceService } from './admin-presence.service';
import { ContactChatService, ContactChatSession, ContactChatSummary } from './contact-chat.service';
import { AssistantMessage, OllamaAssistantService } from './ollama-assistant.service';

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
  protected readonly assistant = inject(OllamaAssistantService);
  protected readonly contactChats = inject(ContactChatService);
  protected readonly chatMessages = signal<AssistantMessage[]>([]);
  protected readonly chatDraft = signal('');
  protected readonly chatPending = signal(false);
  protected readonly chatError = signal(false);
  protected readonly contactChatSession = signal<ContactChatSession | null>(null);
  protected readonly contactChatError = signal(false);
  protected readonly contactChatPending = signal(false);
  protected readonly ownerChats = signal<ContactChatSummary[]>([]);
  protected readonly selectedOwnerChat = signal<ContactChatSession | null>(null);
  protected readonly ownerChatDraft = signal('');
  protected readonly ownerChatPending = signal(false);
  protected readonly ownerChatError = signal(false);
  private readonly destroyRef = inject(DestroyRef);
  private visitorChatTimer?: ReturnType<typeof setInterval>;
  private ownerChatTimer?: ReturnType<typeof setInterval>;
  private assistantConversationId = crypto.randomUUID();

  protected readonly variants: ProfileVariant[] = [
    {
      id: 'data-science-ai',
      label: 'Data Science & AI',
      title: 'Data Science & Artificial Intelligence',
      summary:
        'I combine a software engineering background with data science and AI training to turn data into reliable, production-ready solutions.',
      focusAreas: [
        'Python, NumPy, Pandas and exploratory data analysis',
        'Machine learning and deep learning',
        'SQL and relational databases',
        'Data visualization with Power BI and Tableau',
        'NLP, generative AI and MLOps fundamentals',
        'Software engineering: C#, Angular, APIs',
      ],
      highlights: [
        { label: 'Training', value: 'Sky States DS & AI' },
        { label: 'Core tools', value: 'Python + SQL' },
        { label: 'Foundation', value: 'Software engineering' },
      ],
      experience: [
        {
          title: 'Data Science & AI training',
          period: 'Sky States LLC',
          description: 'Program covering Python, statistics, machine learning, deep learning, NLP with MLOps, data visualization and domain capstone projects.',
        },
        {
          title: 'Software Engineer / Full Stack Developer',
          period: '2021 — Present',
          description: 'Building APIs, internal tools and data-driven workflows that provide the engineering base for analytics and AI solutions.',
        },
      ],
      projects: [
        {
          name: 'Portfolio assistant',
          stack: ['ASP.NET Core', 'Gemini', 'Ollama'],
          summary: 'Public AI assistant that answers questions about the portfolio, using a hosted model on Azure and a local model during development.',
        },
        {
          name: 'Cash Harmony',
          stack: ['Angular', '.NET', 'SQL'],
          summary: 'Operational platform with reporting and workflow visibility, a base for data analysis and dashboards.',
        },
      ],
      resumeLabel: 'Data Science & AI Resume',
      resumeUrl: '_ResumeLast/PDFs/Abel%20Lopez.resume-DS.pdf',
    },
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
      resumeUrl: '_ResumeLast/PDFs/Abel%20Lopez.resume.pdf',
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
      resumeUrl: '_ResumeLast/PDFs/Abel%20Lopez.resume-W.pdf',
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
      resumeUrl: '_ResumeLast/PDFs/Abel%20Lopez.resume.pdf',
    },
  ];

  protected readonly selectedVariantId = signal('data-science-ai');

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
    void this.initializeAdmin();
    this.restoreVisitorChat();
    this.destroyRef.onDestroy(() => {
      if (this.visitorChatTimer) {
        clearInterval(this.visitorChatTimer);
      }

      if (this.ownerChatTimer) {
        clearInterval(this.ownerChatTimer);
      }
    });
  }

  protected selectVariant(variantId: string): void {
    const variant = this.variants.find(item => item.id === variantId) ?? this.variants[0];
    this.selectedVariantId.set(variant.id);
    this.currentVariant.set(variant);
  }

  protected logoutAdmin(): void {
    void this.adminPresence.logout();
  }

  protected updateChatDraft(event: Event): void {
    this.chatDraft.set((event.target as HTMLTextAreaElement).value);
  }

  protected sendChatMessage(event: Event): void {
    event.preventDefault();
    const content = this.chatDraft().trim();

    if (!content || this.chatPending() || this.contactChatPending()) {
      return;
    }

    if (this.contactChatSession()) {
      this.sendVisitorChatMessage(content);
      return;
    }

    const messages = [...this.chatMessages(), { role: 'user' as const, content }];
    this.chatMessages.set(messages);
    this.chatDraft.set('');
    this.chatPending.set(true);
    this.chatError.set(false);

    this.assistant.chat(messages.slice(-16), this.assistantConversationId).subscribe({
      next: response => {
        this.chatMessages.update(current => [
          ...current,
          { role: 'assistant', content: response.reply },
        ]);
        this.chatPending.set(false);
        if (response.contactChatStarted) {
          this.restoreVisitorChat();
        }
      },
      error: () => {
        this.chatError.set(true);
        this.chatPending.set(false);
      },
    });
  }

  protected updateOwnerChatDraft(event: Event): void {
    this.ownerChatDraft.set((event.target as HTMLTextAreaElement).value);
  }

  protected resetVisitorChat(): void {
    this.contactChatSession.set(null);
    this.chatMessages.set([]);
    this.chatDraft.set('');
    this.chatError.set(false);
    this.contactChatError.set(false);
    this.assistantConversationId = crypto.randomUUID();
  }

  protected selectOwnerChat(id: string): void {
    this.contactChats.getOwnerChat(id).subscribe({
      next: session => {
        this.selectedOwnerChat.set(session);
        this.ownerChatError.set(false);
      },
      error: () => this.ownerChatError.set(true),
    });
  }

  protected sendOwnerChatMessage(event: Event): void {
    event.preventDefault();
    const chat = this.selectedOwnerChat();
    const content = this.ownerChatDraft().trim();
    if (!chat || chat.status !== 'Active' || !content || this.ownerChatPending()) {
      return;
    }

    this.ownerChatPending.set(true);
    this.ownerChatError.set(false);
    this.contactChats.sendOwnerMessage(chat.id, content).subscribe({
      next: session => {
        this.selectedOwnerChat.set(session);
        this.ownerChatDraft.set('');
        this.ownerChatPending.set(false);
      },
      error: () => {
        this.ownerChatPending.set(false);
        this.ownerChatError.set(true);
      },
    });
  }

  protected closeOwnerChat(): void {
    const chat = this.selectedOwnerChat();
    if (!chat || this.ownerChatPending()) {
      return;
    }

    this.ownerChatPending.set(true);
    this.contactChats.closeOwnerChat(chat.id).subscribe({
      next: () => {
        this.ownerChatPending.set(false);
        this.refreshOwnerChats();
      },
      error: () => {
        this.ownerChatPending.set(false);
        this.ownerChatError.set(true);
      },
    });
  }

  private async initializeAdmin(): Promise<void> {
    await this.adminPresence.initialize();
    if (this.adminPresence.isAdmin()) {
      this.refreshOwnerChats();
      this.ownerChatTimer = setInterval(() => this.refreshOwnerChats(), 3000);
    }
  }

  private restoreVisitorChat(): void {
    this.contactChats.getVisitorSession().subscribe({
      next: session => {
        this.contactChatSession.set(session);
        if (session && !this.visitorChatTimer) {
          this.visitorChatTimer = setInterval(() => this.refreshVisitorChat(), 3000);
        }
      },
      error: () => this.contactChatError.set(true),
    });
  }

  private refreshVisitorChat(): void {
    this.contactChats.getVisitorSession().subscribe({
      next: session => {
        if (session) {
          this.contactChatSession.set(session);
        }
      },
      error: () => this.contactChatError.set(true),
    });
  }

  private sendVisitorChatMessage(content: string): void {
    this.contactChatPending.set(true);
    this.contactChatError.set(false);
    this.contactChats.sendVisitorMessage(content).subscribe({
      next: session => {
        this.contactChatSession.set(session);
        this.chatDraft.set('');
        this.contactChatPending.set(false);
      },
      error: () => {
        this.contactChatPending.set(false);
        this.contactChatError.set(true);
      },
    });
  }

  private refreshOwnerChats(): void {
    this.contactChats.listOwnerChats().subscribe({
      next: sessions => {
        this.ownerChats.set(sessions);
        this.ownerChatError.set(false);
        const selected = this.selectedOwnerChat();
        if (selected) {
          this.contactChats.getOwnerChat(selected.id).subscribe({
            next: session => this.selectedOwnerChat.set(session),
            error: () => this.ownerChatError.set(true),
          });
        } else {
          const next = sessions.find(session => session.status === 'Active') ??
            sessions.find(session => session.status === 'Waiting');
          if (next) {
            this.selectOwnerChat(next.id);
          }
        }
      },
      error: () => this.ownerChatError.set(true),
    });
  }
}
