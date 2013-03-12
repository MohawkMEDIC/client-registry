--
-- Copyright 2012-2013 Mohawk College of Applied Arts and Technology
-- 
-- Licensed under the Apache License, Version 2.0 (the "License"); you 
-- may not use this file except in compliance with the License. You may 
-- obtain a copy of the License at 
-- 
-- http://www.apache.org/licenses/LICENSE-2.0 
-- 
-- Unless required by applicable law or agreed to in writing, software
-- distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
-- WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
-- License for the specific language governing permissions and limitations under 
-- the License.
-- 
-- User: fyfej
-- Date: 5-12-2012
--

-- CONTENTS FROM ICD 10 CODE SYSTEM 
-- COPYRIGHT (C) WORLD HEALTH ORGANIZATION
SELECT QDCDB_REG_CD('Z67','2.16.840.1.113883.6.90','Blood Type',NULL);
SELECT QDCDB_REG_CD('Z67.1','2.16.840.1.113883.6.90','Blood type,  Type A blood',QDCDB_GET_CD('2.16.840.1.113883.6.90','Z67'));
SELECT QDCDB_REG_CD('Z67.10','2.16.840.1.113883.6.90','Blood type,  Type A blood,  Rh positive',QDCDB_GET_CD('2.16.840.1.113883.6.90','Z67.1'));
SELECT QDCDB_REG_CD('Z67.11','2.16.840.1.113883.6.90','Blood type,  Type A blood,  Rh Negative',QDCDB_GET_CD('2.16.840.1.113883.6.90','Z67.1'));
SELECT QDCDB_REG_CD('Z67.2','2.16.840.1.113883.6.90','Blood type,  Type B blood',QDCDB_GET_CD('2.16.840.1.113883.6.90','Z67'));
SELECT QDCDB_REG_CD('Z67.20','2.16.840.1.113883.6.90','Blood type,  Type B blood,  Rh positive',QDCDB_GET_CD('2.16.840.1.113883.6.90','Z67.2'));
SELECT QDCDB_REG_CD('Z67.21','2.16.840.1.113883.6.90','Blood type,  Type B blood,  Rh negative',QDCDB_GET_CD('2.16.840.1.113883.6.90','Z67.2'));
SELECT QDCDB_REG_CD('Z67.3','2.16.840.1.113883.6.90','Blood type,  Type AB blood',QDCDB_GET_CD('2.16.840.1.113883.6.90','Z67'));
SELECT QDCDB_REG_CD('Z67.30','2.16.840.1.113883.6.90','Blood type,  Type AB blood,  Rh positive',QDCDB_GET_CD('2.16.840.1.113883.6.90','Z67.3'));
SELECT QDCDB_REG_CD('Z67.31','2.16.840.1.113883.6.90','Blood type,  Type AB blood,  Rh negative',QDCDB_GET_CD('2.16.840.1.113883.6.90','Z67.3'));
SELECT QDCDB_REG_CD('Z67.4','2.16.840.1.113883.6.90','Blood type,  Type O blood',QDCDB_GET_CD('2.16.840.1.113883.6.90','Z67'));
SELECT QDCDB_REG_CD('Z67.40','2.16.840.1.113883.6.90','Blood type,  Type O blood,  Rh positive',QDCDB_GET_CD('2.16.840.1.113883.6.90','Z67.4'));
SELECT QDCDB_REG_CD('Z67.41','2.16.840.1.113883.6.90','Blood type,  Type O blood,  Rh negative',QDCDB_GET_CD('2.16.840.1.113883.6.90','Z67.4'));
SELECT QDCDB_REG_CD('Z67.9','2.16.840.1.113883.6.90','Blood type,  Unspecified blood type',QDCDB_GET_CD('2.16.840.1.113883.6.90','Z67'));
SELECT QDCDB_REG_CD('Z67.90','2.16.840.1.113883.6.90','Blood type,  Unspecified blood type,  Rh positive',QDCDB_GET_CD('2.16.840.1.113883.6.90','Z67.9'));
SELECT QDCDB_REG_CD('Z67.91','2.16.840.1.113883.6.90','Blood type,  Unspecified blood type,  Rh negative',QDCDB_GET_CD('2.16.840.1.113883.6.90','Z67.9'));
