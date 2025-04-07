import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';
import { RouterOutlet } from '@angular/router'; // Import RouterOutlet
import { NavMenuComponent } from './nav-menu/nav-menu.component'; // Import NavMenu
import { Component } from '@angular/core';
import { By } from '@angular/platform-browser';
import { provideRouter, Routes } from '@angular/router';
import {provideHttpClientTesting} from '@angular/common/http/testing';
import {provideHttpClient} from '@angular/common/http';

@Component({ template: '' })
class DummyComponent {}

describe('AppComponent', () => {
  let component: AppComponent;
  let fixture: ComponentFixture<AppComponent>;

  beforeEach(async () => {
    const testRoutes: Routes = [
      { path: '', component: DummyComponent },
      { path: 'user-session', component: DummyComponent },
    ];
    await TestBed.configureTestingModule({
      imports: [AppComponent, RouterOutlet, NavMenuComponent],
      providers: [provideRouter(testRoutes), provideHttpClient(),provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the app', () => {
    expect(component).toBeTruthy();
  });

  it(`should have the 'angular.ssr' title`, () => {
    expect(component.title).toEqual('angular.ssr');
  });

  it('should render nav-menu', () => {
    const navMenu = fixture.debugElement.query(By.directive(NavMenuComponent));
    expect(navMenu).toBeTruthy();
  });

  it('should render router-outlet', () => {
    const routerOutlet = fixture.debugElement.query(By.directive(RouterOutlet));
    expect(routerOutlet).toBeTruthy();
  });
});
