import { Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { LogoComponent } from '../../shared/components/logo/logo.component';

@Component({
  selector: 'app-splash',
  standalone: true,
  imports: [LogoComponent],
  templateUrl: './splash.component.html',
  styleUrl: './splash.component.css'
})
export class SplashComponent implements OnInit, OnDestroy {
  @Output() splashDone = new EventEmitter<void>();

  isExiting = false;
  private timers: ReturnType<typeof setTimeout>[] = [];

  ngOnInit(): void {
    this.timers.push(setTimeout(() => {
      this.isExiting = true;
    }, 2200));

    this.timers.push(setTimeout(() => {
      this.splashDone.emit();
    }, 2700));
  }

  ngOnDestroy(): void {
    this.timers.forEach(timer => clearTimeout(timer));
  }
}
