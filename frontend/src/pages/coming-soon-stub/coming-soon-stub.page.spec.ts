import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { ComingSoonStubPage } from './coming-soon-stub.page';

describe('ComingSoonStubPage', () => {
  it('renders the eyebrow/heading and guidance text from route data', () => {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { data: { title: 'Resumes' } } },
        },
      ],
    });

    const fixture = TestBed.createComponent(ComingSoonStubPage);
    fixture.detectChanges();
    const text: string = fixture.nativeElement.textContent;

    expect(fixture.nativeElement.querySelectorAll('h1')).toHaveLength(1);
    expect(text).toContain('Resumes');
    expect(text).toContain('Coming soon');
    expect(text).toContain('Resumes is on its way');
  });
});
