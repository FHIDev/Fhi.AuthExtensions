import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { NgIf, NgFor } from '@angular/common';
import { map, catchError, of } from 'rxjs';

interface Record {
    createdAt: string;
    name: string;
    description: string;
  }

@Component({
    selector: 'healthrecord-component',
    templateUrl: 'health-record.component.html',
    imports: [NgIf, NgFor]
})

export class HealthRecordComponent implements OnInit{
    constructor(private http: HttpClient) {}
  public records: Record[] = [];

  ngOnInit() {
  }

  getRecords() {
    this.http.get<Record[]>('/bff/v1/health-records', { withCredentials: true }).pipe(
      map(result => {
        return result;
      }),
      catchError(error => {
        console.error(error);
        return of([]);
      })
    ).subscribe(result => {
      this.records = result;
      });
      
  }
}
