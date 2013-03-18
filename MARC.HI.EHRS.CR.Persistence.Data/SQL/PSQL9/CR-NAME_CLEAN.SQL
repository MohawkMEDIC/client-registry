/* 
 * POSTGRECR - MARC-HI CLIENT REGISTRY DATABASE SCHEMA FOR POSTGRESQL
 * VERSION: 2.0
 * AUTHOR: JUSTIN FYFE
 * DATE: JULY 12, 2012
 * FILES:
 *POSTGRECR-DDL.SQL- SQL CODE TO CREATE TABLES, INDECIES, VIEWS AND SEQUENCES
 *POSTGRECR-SRCH.SQL- SQL CODE TO CREATE SEARCH FUNCTIONS
 *POSTGRESHR-FX.SQL- SQL CODE TO CREATE SUPPORT PROCEDURES AND FUNCTIONS
 * DESCRIPTION:
 *THIS FILE CLEANS AND THEN RE-CREATES THE POSTGRESQL CLIENT REGISTRY
 *PERSON NAME SYNONYMN TABLES. 
 * LICENSE:
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
 
 DROP TABLE NAME_CMP_SYN_CDTBL;
 
